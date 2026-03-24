import { BackendApplicationContribution } from '@theia/core/lib/node';
import { ContainerModule } from '@theia/core/shared/inversify';
import { SearchStudioBackendApplicationContribution } from './search-studio-backend-application-contribution';

/**
 * Registers the minimal backend services that the fresh Studio shell needs during the bootstrap slice.
 */
export default new ContainerModule(bind => {
    // Register the backend contribution once so the same-origin runtime configuration route is mapped exactly once.
    bind(SearchStudioBackendApplicationContribution).toSelf().inSingletonScope();
    bind(BackendApplicationContribution).toService(SearchStudioBackendApplicationContribution);
});
