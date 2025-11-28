using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


namespace Tanks.Complete
{
    /// <summary>
    ///     Handle the tank control when the tank is set to Computer controlled
    /// </summary>
    public class TankAI : MonoBehaviour
    {

        private readonly float m_TimeBetweenShot = 2.0f; // The AI Tank have a cooldown on shot to avoid spamming shot

        private GameObject[] m_AllTanks; // List of all the tanks in the scene.
        private int m_CurrentCorner;     // Which corner of the path the tank is currently going forward to

        private NavMeshPath m_CurrentPath; // The current path followed by the tank.

        private State m_CurrentState = State.Seek; // The current AI state the Tank is in.

        private Transform m_CurrentTarget; // Which Transform the tank is following

        private Vector3 m_FleeingLastPosition; // Used to check how far we moved as we flee. If this doesn't change for a while, need to pick another point
        private bool m_IsMoving;               // Is the tank currently moving or not (the tank stop to shoot)

        private Vector3 m_LastTargetPosition; // The position of the target last frame
        private float m_MaxShootingDistance;  // Store the max shooting distance based on TankShooting settings

        private TankMovement m_Movement; // Reference to the movement script

        private float m_PathfindTime = 0.5f; // Only trigger a pathfind after this time, to not degrade performance
        private float m_PathfindTimer;       // The time until the next pathfind call
        private TankShooting m_Shooting;     // Reference to the shooting script
        private float m_ShotCooldown;        // The remaining time until the next shot
        private float m_SinceLastFleeingMove;
        private float m_TimeCloseToTarget;
        private float m_TimeSinceLastTargetMove; // Timer counting how long the target hasn't moved. This is used to trigger the flee state

        private void Awake()
        {
            //Awake is still called on disabled component. So that the user can test disabling AI on a single tank
            //we ensure that the component wasn't disabled before initializing everything
            if (!isActiveAndEnabled)
                return;

            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();

            // ensure that both movement and shooting script are set in "computer controlled" mode
            m_Movement.m_IsComputerControlled = true;
            m_Shooting.m_IsComputerControlled = true;

            // to avoid all computer controlled tank pathfinding together (and taxing the CPU), AI tank have a random
            // pathfinding time that will stagger them across multiple frame
            m_PathfindTime = Random.Range(0.3f, 0.6f);

            // Compute and store what is the maximum distance a shot from this tank can reach. This will be used when deciding when
            // to start charging and when to release a shot
            m_MaxShootingDistance = Vector3.Distance(m_Shooting.GetProjectilePosition(1.0f), transform.position);

            // We use FindObjectByType to get all Tanks, to not depend on GameManager so user can try adding AI in an
            // empty scene where no GameManager was added yet.
            m_AllTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(e => e.gameObject).ToArray();
        }

        private void Update()
        {
            // If there is a cooldown active, we decrement it by the time elapsed since last frame
            if (m_ShotCooldown > 0)
                m_ShotCooldown -= Time.deltaTime;

            // increment the time since last pathfind. The SeekUpdate will check if it goes over the pathfinding time
            // and if it need to trigger a new pathfinding
            m_PathfindTimer += Time.deltaTime;

            switch (m_CurrentState)
            {
                case State.Seek:
                    SeekUpdate();
                    break;
                case State.Flee:
                    FleeUpdate();
                    break;
            }
        }

        // Contrary to Update (which is called every new frame, so called a variable amount of time per second depending
        // if the game is rendering fast or not), FixedUpdate is called at a given interval define in the Physic Setting
        // of the project. This is where all physic code should be placed.
        private void FixedUpdate()
        {
            // If the tank doesn't have a path currently, exit early.
            if (m_CurrentPath == null || m_CurrentPath.corners.Length == 0)
                return;

            var rb = m_Movement.Rigidbody;

            //The point we will orient toward. By default, the current corner in our path
            var orientTarget = m_CurrentPath.corners[Mathf.Min(m_CurrentCorner, m_CurrentPath.corners.Length - 1)];

            //if we are not moving, we orient toward our target instead
            if (!m_IsMoving && m_CurrentTarget != null)
                orientTarget = m_CurrentTarget.position;

            var toOrientTarget = orientTarget - transform.position;
            toOrientTarget.y = 0;
            toOrientTarget.Normalize();

            var forward = rb.rotation * Vector3.forward;

            var orientDot = Vector3.Dot(forward, toOrientTarget);
            var rotatingAngle = Vector3.SignedAngle(toOrientTarget, forward, Vector3.up);

            //if we are moving we move in our forward direction by our max speed
            var moveAmount = Mathf.Clamp01(orientDot) * m_Movement.m_Speed * Time.deltaTime;
            if (m_IsMoving && moveAmount > 0.000001f)
            {
                rb.MovePosition(rb.position + forward * moveAmount);
            }

            //the actual rotation for that frame is the smallest between the max turning speed for that time frame and the
            //angle itself. Multiplied by the sign of the angle to ensure we rotate in the right direction
            rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), m_Movement.m_TurnSpeed * Time.deltaTime);

            if (Mathf.Abs(rotatingAngle) > 0.000001f)
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(-rotatingAngle, Vector3.up));

            // If we reached our current target, we increase our corner. We will never reach the target when the target
            // is another tank as we stop before.
            if (Vector3.Distance(rb.position, orientTarget) < 0.5f)
            {
                m_CurrentCorner += 1;
            }
        }

        // If a GameManager exist, it will call this function after creating a computer controlled tank. This just replace
        // the list of tanks with the one from the GameManager
        public void Setup(GameManager manager)
        {
            // If this was using manager.m_SpawnPoints.ToArray(), it will get an array of TankManager, but m_AllTanks is an array of Transform.
            // The Select function will call the function passed as a parameter on each entry in the list (here TankManager) and make a new list
            // containing what each return. The function we pass here, e => e.m_Instance, return the Transform of the tank the TankManager manage
            // so effectively manager.m_SpawnPoints.Select(e => e.m_Instance) give a list of all the tanks transform.
            m_AllTanks = manager.m_SpawnPoints.Select(e => e.m_Instance).ToArray();
        }

        public void TurnOff()
        {
            enabled = false;
        }

        private void SeekUpdate()
        {
            // To lighten the load on the CPU the tanks do not pathfind to their target every single frame. Instead, they
            // wait a bit between each pathfind. They will go toward an "outdated" position in between, but as the pathfind time is
            // under 1s, this is visually not noticeable and a lot more efficient than trying to pathfinding 30+ time each second
            if (m_PathfindTimer > m_PathfindTime)
            {
                // reset the time since last pathfind
                m_PathfindTimer = 0.0f;

                // Store which target we had, as we use this to test if we changed target later
                var previousTarget = m_CurrentTarget;

                // Check all tank to find the closest
                var distance = float.MaxValue;
                foreach (var tank in m_AllTanks)
                {
                    // skip that tank if it's null (destroyed)
                    if (tank == null)
                        continue;

                    // skip that tank if it's ourself
                    if (tank == gameObject)
                        continue;

                    // skip that tank if it's been disabled (e.g. dead)
                    if (!tank.activeSelf)
                        continue;

                    var tankDistance = Vector3.Distance(tank.transform.position, transform.position);
                    if (tankDistance < distance)
                    {
                        distance = tankDistance;
                        m_CurrentTarget = tank.transform;
                    }
                }

                // If we haven't found any tank, we stop searching
                if (m_CurrentTarget == null)
                    return;

                // Create a navmesh path for our current path if it doesn't exist yet
                if (m_CurrentPath == null)
                {
                    m_CurrentPath = new NavMeshPath();
                }

                // if the target changed (e.g. another tank is now closer to us) OR the current path is empty (there was
                // no path or we reached the end of our path) we need to calculate a new path
                if (previousTarget != m_CurrentTarget || m_CurrentPath.corners.Length == 0)
                {
                    // Compute a path toward that target
                    NavMesh.CalculatePath(transform.position, m_CurrentTarget.position, NavMesh.AllAreas, m_CurrentPath);

                    // We start at corners 1, as corners 0 is the starting point (i.e. where we are currently)
                    m_CurrentCorner = 1;

                    // If we changed target, we reset how long we were close and how long the target haven't moved
                    m_TimeCloseToTarget = 0.0f;
                    m_TimeSinceLastTargetMove = 0.0f;
                }
                else
                {
                    // Compute how much the target moved since last pathfind
                    var distMoved = Vector3.Distance(m_CurrentTarget.position, m_LastTargetPosition);

                    // if the target moved at least a bit we check if we need a new path. As our corner 0 is the start position
                    // and our current corners is always the corner of the path where we are going toward, we need at least
                    // 2 corners (start and target) for that test to be valid.
                    if (distMoved > 0.01f && m_CurrentPath.corners.Length >= 2)
                    {
                        // We calculate a new path only if the difference of position of the target is closer to our current target
                        // than us.
                        // In simple term : we don't care if the target move in a direction that doesn't impact us reaching the last
                        // target point, as the target will still need to get through us anyway. However, if it get closer to
                        // our target point than us, we may miss them as they are "cutting" in front of us.
                        var distToTargetFromLastPath =
                            Vector3.Distance(m_CurrentTarget.position, m_CurrentPath.corners.Last());
                        var distToTargetFromUs = Vector3.Distance(m_CurrentPath.corners.Last(), transform.position);

                        if (distToTargetFromLastPath < distToTargetFromUs)
                        {
                            // Same as above, compute a path toward our target
                            NavMesh.CalculatePath(transform.position, m_CurrentTarget.position, NavMesh.AllAreas,
                                m_CurrentPath);
                            m_CurrentCorner = 1;
                        }
                    }
                }

                // Tank is moving by default, as soon as it start charging (in next section of this function) it will switch
                // to not moving.
                m_IsMoving = true;
            }

            // if our current target got destroyed (or was otherwise set to null), just exit the function.
            if (m_CurrentTarget == null)
                return;

            // Track how long the target hasn't moved. If it hasn't moved for a while, it mean it's probably charging a shot
            // facing us, and we don't want to just stand still waiting to take its shot
            if (Vector3.Distance(m_CurrentTarget.position, m_LastTargetPosition) > 0.1f)
            {
                m_TimeSinceLastTargetMove = 0.0f;
            }
            else
            {
                m_TimeSinceLastTargetMove += Time.deltaTime;
            }

            m_LastTargetPosition = m_CurrentTarget.position;

            // Get a vector from this tank to its target
            var toTarget = m_CurrentTarget.position - transform.position;
            // by setting y to 0, we ensure that the vector to the target is in the flat plane of the ground
            toTarget.y = 0;

            var targetDistance = toTarget.magnitude;
            // normalize the vector to the target, setting its length to 1, which is useful for some mathematical operations.
            toTarget.Normalize();

            if (targetDistance < 3.0f)
            {
                // Count how long we've been very close to the target
                m_TimeCloseToTarget += Time.deltaTime;

                // ... if we stay close to the target more than 2s, we're probably running in circle around it, so
                // flee instead to try to get more space to aim at it
                if (m_TimeCloseToTarget > 2.0f)
                {
                    StartFleeing();
                    return;
                }
            }
            else
            {
                m_TimeCloseToTarget = 0.0f;
            }

            // the dot product between 2 normalized vector is the cosine of the angle between those vector. This is useful as it
            // allow to test how aligned those vector are : 1 -> in the same direction, 0 -> 90 deg angle, -1, pointing in opposite direction.
            // As we compute the dot product between our forward vector and the vector toward our target, this give use how much we are
            // facing our target : if this is close to 1, we are facing straight at our target.
            var dotToTarget = Vector3.Dot(toTarget, transform.forward);

            //if we are charging, check if the current shot can reach the target
            if (m_Shooting.IsCharging)
            {
                // get the estimated point of the projectile with the current charging value
                var currentShotTarget = m_Shooting.GetProjectilePosition(m_Shooting.CurrentChargeRatio);
                // the distance from us to that estimated point
                var currentShotDistance = Vector3.Distance(currentShotTarget, transform.position);

                //if we are facing the target and our shot is charged enough to reach the target, release the shot
                // note : we remove 2 from the target distance as our shot have splash damage, so we can release the
                // shot earlier
                if (currentShotDistance >= targetDistance - 2 && dotToTarget > 0.99f)
                {
                    m_IsMoving = false;
                    m_Shooting.StopCharging();

                    // we just shot, so we set the cooldown to the time between shot (this is decremented each frame in the update function)
                    m_ShotCooldown = m_TimeBetweenShot;

                    // We just shot, and our target haven't moved for a while. Which mean they are probably also aiming and shooting at us
                    // we go into fleeing mode instead of staying there as a static target
                    if (m_TimeSinceLastTargetMove > 2.0f)
                    {
                        StartFleeing();
                    }
                }
            }
            else
            {
                // We aren't charging yet, so check if the target is closer than our max shooting distance, which mean we can start charging the shot
                // (a "smarter" solution would be to compute how early we can charge so we reach max distance already max charged)
                if (targetDistance < m_MaxShootingDistance)
                {
                    // This use the navmesh to check if there are any obstacle between us and the target. If this return false
                    // this mean there is no unobstructed path, so there *is* an obstacle, so we shouldn't start shooting yet
                    if (!NavMesh.Raycast(transform.position, m_CurrentTarget.position, out var hit, ~0))
                    {
                        // we stop moving as we can reach our target with our shot
                        m_IsMoving = false;

                        // if our cooldown is not 0 or below, we have to wait for it to be before shooting.
                        // If it is below 0, and we have shells (using WeaponStockData), we start charging the shot
                        if (m_ShotCooldown <= 0.0f && m_Shooting.CurrentShells > 0)
                        {
                            m_Shooting.StartCharging();
                        }
                    }
                }
            }
        }

        private void FleeUpdate()
        {
            // When fleeing the tank will go toward a random point away from its target. When we reach the last corners
            // (i.e. point) of that path, we can go back to seek mode
            if (m_CurrentCorner >= m_CurrentPath.corners.Length)
                m_CurrentState = State.Seek;

            //check how far we moved since last update, if we don't move enough for a while, we're stuck somewhere, pick another point
            var distance = (transform.position - m_FleeingLastPosition).magnitude;
            m_FleeingLastPosition = transform.position;

            if (distance < 0.001f)
            {
                m_SinceLastFleeingMove += Time.deltaTime;
            }
            else
            {
                m_SinceLastFleeingMove = 0;
            }

            if (m_SinceLastFleeingMove > 2.0f)
            {
                StartFleeing();
            }
        }

        private void StartFleeing()
        {
            // To flee, we need to pick a point away from our current target
            // 現在のターゲットがnullの場合は何もしない
            if (m_CurrentTarget == null)
                return;

            m_FleeingLastPosition = transform.position;
            m_SinceLastFleeingMove = 0.0f;

            // Start by getting the vector *toward* our target...
            var toTarget = (m_CurrentTarget.position - transform.position).normalized;

            // then rotate that vector of a random angle between 90 and 180 degree, which will give us a random direction
            // in the opposite direction
            toTarget = Quaternion.AngleAxis(Random.Range(90.0f, 180.0f) * Mathf.Sign(Random.Range(-1.0f, 1.0f)),
                Vector3.up) * toTarget;

            // then we pick a point in that random direction at a random distance between 5 and 20 units
            toTarget *= Random.Range(5.0f, 20.0f);

            // Finally we compute a path toward that random point, which become our new current path.
            if (NavMesh.CalculatePath(transform.position, transform.position + toTarget, NavMesh.AllAreas,
                    m_CurrentPath))
            {
                m_CurrentState = State.Flee;
                m_CurrentCorner = 1;

                m_IsMoving = true;
            }
        }

        // Utility function which will add the length of all the sections of the given path to get its effective length
        private float GetPathLength(NavMeshPath path)
        {
            float dist = 0;
            for (var i = 1; i < path.corners.Length; ++i)
            {
                dist += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }

            return dist;
        }

        // Possible state of the Computer controlled tank : either seeking itsd target or fleeing from it
        private enum State
        {
            Seek,
            Flee
        }
    }
}
