# Frontend — CLAUDE.md

You are a React / TypeScript developer. Rules are absolute. Fix code, never weaken rules.

## Stack

- React 19, Vite, TypeScript (strict mode globally)
- React Router, Zustand (client state), TanStack Query (server state, optional)
- One UI library per file — Ant Design **or** Tailwind. If `antd` is imported in a file, that file uses only AntD layout/spacing (no Tailwind utility classes). If Tailwind is used for layout, no `antd` import. Pick one per file.

## API contract (single source of truth)

- API types and services are **generated** from backend OpenAPI. Never hand-written.
- Generator: `openapi-typescript-codegen` (or equivalent). Output: `src/api/generated/` (read-only).
- Script: `npm run generate-api` pulls from `${VITE_API_URL}/swagger/v1/swagger.json`.
- Generated services read `VITE_API_URL` and inject JWT Bearer from the auth store automatically.
- If a backend type is missing on the frontend, regenerate — do not transcribe.

## Folder convention (paths computable from name)

| Kind | Path |
|---|---|
| Page | `src/pages/<area>/<PageName>.tsx` |
| Shared component | `src/components/<group>/<Name>.tsx` |
| Hook | `src/hooks/use<Name>.ts` |
| Zustand store | `src/store/<name>.ts` exports `use<Name>Store` |
| Generated API | `src/api/generated/` (do not edit) |
| Hand-written API wrapper | `src/api/client.ts` |
| Route config | `src/router.tsx` |
| Runtime config | `src/config.ts` (reads `import.meta.env.VITE_*`) |
| Global styles | `src/index.css` |
| Non-API domain types | `src/types/<name>.ts` |

## Naming (strict)

- Components: `PascalCase`. File name == default export.
- Hooks: `useCamelCase`, file `useCamelCase.ts`.
- Stores: file lowercase (`auth.ts`), exported hook `use<Pascal>Store` (`useAuthStore`).
- Pages: `PascalCase`, no `Page` suffix.
- Module-level **primitive** constants (string, number, boolean, simple frozen primitive collections) in `SCREAMING_SNAKE_CASE` (e.g., `API_URL`, `MAX_RETRY`, `DEFAULT_LOCALE`). Module-level objects, arrays, and complex data structures use `camelCase` (e.g., `adminCards`, `exerciseTypes`).
- Types/interfaces: `PascalCase`, no `I` prefix.

One component per file. One hook per file. One store per file.

## Mandatory rules

- TypeScript strict mode on globally. Never weaken per file or directory.
- No manual API request/response types. Everything from `src/api/generated/`.
- No direct HTTP from components. Components call hooks; hooks call generated services.
- No `fetch` / `axios` inline in components or pages.
- No `any`, `as any`, `as unknown as`, `// @ts-ignore` to silence types.
- Stores own persisted state. Components never touch `localStorage` / `sessionStorage` directly.
- All routes in `src/router.tsx`. No ad-hoc `<BrowserRouter>` in components.
- All env reads in `src/config.ts`. Components import from `config.ts`, not `import.meta.env`.

## Workflow

1. `npm run generate-api` if backend OpenAPI changed.
2. `npm run lint` → must pass.
3. `npm run build` (`tsc -b && vite build`) → must succeed (typecheck + bundle).

**Definition of Done**: lint green, build green. Partial = not done.

## Never

- Hand-write request/response/DTO types that mirror backend.
- Edit anything inside `src/api/generated/`.
- Add a new dependency without user approval.
- Use `any` / `as any` / `// @ts-ignore` to silence types.
- Call backend via `fetch` / `axios` from a component or page.
- Read `localStorage` / `sessionStorage` outside a store.
- Read `import.meta.env` outside `src/config.ts`.
- Add a page not registered in `src/router.tsx`.
- Add a second state-management library, routing library, or UI framework.
- Use inline `style={{}}` beyond a single one-off property.
