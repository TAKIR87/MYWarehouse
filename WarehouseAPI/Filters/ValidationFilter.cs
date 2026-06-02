using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WarehouseAPI.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        // Собираем все ошибки валидации в удобный словарь
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        context.Result = new BadRequestObjectResult(new
        {
            status  = 400,
            message = "Ошибка валидации",
            errors
        });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}