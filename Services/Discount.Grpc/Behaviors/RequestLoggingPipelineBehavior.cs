using MediatR;
using Serilog.Context;

namespace Discount.Grpc.Behaviors;

public sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>
    (ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    // where TResponse : Result
{


    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Processing request {RequestName}", requestName);

        var result = await next();

        if (result.ToString() == "Success")
        {
            logger.LogInformation("Completed request {RequestName}", request);
        }
        else
        {
            using (LogContext.PushProperty("Error", result.ToString(), true))
            {
                logger.LogError("Completed request {RequestName} with error", requestName);
            }
        }

        return result;
    }
}