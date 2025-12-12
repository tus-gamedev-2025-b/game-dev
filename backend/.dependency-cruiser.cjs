/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
  forbidden: [
    // ===== Clean Architecture Rules =====
    // Layer hierarchy (inner to outer):
    // 1. domain (entities, repository interfaces, validators)
    // 2. usecases (application business rules)
    // 3. infra (repository implementations, external services)
    // 4. routes/helpers/schemas (interface adapters)
    // 5. libs (frameworks & drivers)

    // Domain layer should not depend on any outer layers
    {
      name: "domain-no-outer-deps",
      comment:
        "Domain layer must not depend on usecases, routes, infra, libs, helpers, or schemas",
      severity: "error",
      from: { path: "^app/domain/" },
      to: {
        path: [
          "^app/usecases/",
          "^app/routes/",
          "^app/infra/",
          "^app/libs/",
          "^app/helpers/",
          "^app/schemas/",
        ],
      },
    },

    // Domain should not depend on external frameworks (except types)
    {
      name: "domain-no-framework-deps",
      comment:
        "Domain layer should not import framework code (hono, drizzle implementations)",
      severity: "error",
      from: { path: "^app/domain/" },
      to: {
        path: ["node_modules/hono", "node_modules/@hono"],
      },
    },

    // Usecases should not depend on routes (interface layer)
    {
      name: "usecases-no-routes-deps",
      comment: "Usecase layer must not depend on routes (interface layer)",
      severity: "error",
      from: { path: "^app/usecases/" },
      to: { path: "^app/routes/" },
    },

    // Usecases should not depend on libs directly (should use repository interfaces via infra)
    {
      name: "usecases-no-libs-deps",
      comment:
        "Usecase layer should not directly depend on libs (infrastructure)",
      severity: "error",
      from: {
        path: "^app/usecases/",
        pathNot: "\\.test\\.ts$", // Allow test files to import libs for setup
      },
      to: {
        path: "^app/libs/",
        pathNot: "^app/libs/cache/", // Allow cache for now (could be moved to infra)
      },
    },

    // Usecases should not depend on HTTP-related schemas
    {
      name: "usecases-no-http-schemas",
      comment: "Usecase layer should not depend on HTTP schemas",
      severity: "warn",
      from: { path: "^app/usecases/" },
      to: { path: "^app/schemas/" },
    },

    // Routes should not depend on libs/db directly (use usecases instead)
    {
      name: "routes-no-direct-db",
      comment:
        "Routes should not directly access database, use usecases instead",
      severity: "error",
      from: { path: "^app/routes/" },
      to: { path: "^app/libs/db/" },
    },

    // Routes should not depend on infra directly (use usecases instead)
    {
      name: "routes-no-infra",
      comment:
        "Routes should not directly access infrastructure, use usecases instead",
      severity: "error",
      from: { path: "^app/routes/" },
      to: { path: "^app/infra/" },
    },

    // ===== General Best Practices =====

    // No circular dependencies
    {
      name: "no-circular",
      comment: "No circular dependencies allowed",
      severity: "error",
      from: {},
      to: { circular: true },
    },

    // No orphan modules (files that are not imported anywhere)
    {
      name: "no-orphans",
      comment: "No orphan modules",
      severity: "warn",
      from: {
        orphan: true,
        pathNot: [
          "(^|/)\\.[^/]+\\.(c?js|ts|json)$", // dotfiles
          "\\.d\\.ts$", // TypeScript declaration files
          "(^|/)tsconfig\\.json$",
          "(^|/)biome\\.json$",
          "^index\\.ts$", // entry point
          "\\.test\\.ts$", // test files
          "^app/domain/.*/validator\\.ts$", // validators may be used only in tests
        ],
      },
      to: {},
    },
  ],

  options: {
    doNotFollow: {
      path: ["node_modules"],
    },
    tsPreCompilationDeps: true,
    tsConfig: {
      fileName: "tsconfig.json",
    },
    enhancedResolveOptions: {
      exportsFields: ["exports"],
      conditionNames: ["import", "require", "node", "default"],
      mainFields: ["main", "types"],
    },
    reporterOptions: {
      dot: {
        collapsePattern: "node_modules/(@[^/]+/[^/]+|[^/]+)",
      },
      text: {
        highlightFocused: true,
      },
    },
  },
}
