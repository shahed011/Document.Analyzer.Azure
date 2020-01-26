using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace DocumentAnalyzer.Api.Common.Validation
{
    public class ValidateModelStateFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext == null) throw new ArgumentNullException(nameof(context));
            if (!context.ModelState.IsValid)
            {
                // This should only be invoked if the request is malformed, otherwise
                // the ValidateRequestFilter will take care of validation
                var errorResponse = new ErrorResponse(
                    context.HttpContext.TraceIdentifier,
                    "request_invalid",
                    new[] { "request_body_malformed " }
                );

                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
