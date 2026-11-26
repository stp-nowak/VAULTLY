// Public API of the console-admin domain slice: who may use the Identity service's admin console.
export { consoleRoutes, getConsoleAccess } from './api/consoleAdminApi';
export { consoleAccessQueryOptions, consoleAdminQueryKeys } from './api/consoleAdminQueries';
export type { ConsoleAccess, ConsoleAccessStatus, ConsoleAdmin } from './model/types';
