using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Action.Platform.DependencyInjection.Attributes;
using Action.Platform.Logging.Extensions;
using Action.Platform.WebApp.Implementations.Status;
using Action.Search.DocumentExportService.ApplicationServices.Services.Interfaces;


namespace WebApp.Middlewares
{
    [NoAutoRegistration]
    [ExcludeFromCodeCoverage]
    internal sealed class CurrentStatusMiddleware : StatusMiddleware

    {
        private readonly IStatusService _statusService;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public CurrentStatusMiddleware(IStatusService statusService)
        {
            _statusService = statusService;
        }

        protected override async Task<bool> DoCheckStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _statusService.CheckStatusAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ex.Log();
            }

            return false;
        }
    }
}