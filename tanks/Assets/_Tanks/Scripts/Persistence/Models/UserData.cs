using System;
using Tanks.ApiClient.Model;
using UnityEngine;

namespace Tanks.Complete.Persistence.Models
{
    [Serializable]
    public class UserData
    {
        public int UserId;
        public string UserName;
        public int Wins;
        public int Losses;
        public string CreatedAtIso;
        public string UpdatedAtIso;

        public bool IsValid => UserId > 0 && !string.IsNullOrEmpty(UserName);

        public DateTimeOffset CreatedAt => TryParse(CreatedAtIso);

        public DateTimeOffset UpdatedAt => TryParse(UpdatedAtIso);

        public static UserData FromAuthResponse(AuthResponse response)
        {
            return response == null ? null : FromAuthResponseUser(response.User);
        }

        public static UserData FromAuthResponseUser(AuthResponseUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserData
            {
                UserId = Convert.ToInt32(user.Id),
                UserName = user.Name,
                Wins = Convert.ToInt32(user.Wins),
                Losses = Convert.ToInt32(user.Losses),
                CreatedAtIso = user.CreatedAt,
                UpdatedAtIso = user.UpdatedAt
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static UserData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<UserData>(json);
        }

        private static DateTimeOffset TryParse(string iso)
        {
            if (DateTimeOffset.TryParse(iso, out var result))
            {
                return result;
            }

            return DateTimeOffset.MinValue;
        }
    }
}
