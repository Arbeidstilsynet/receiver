using System.Text.Json;
using Arbeidstilsynet.Receiver.Model.Request;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.ModelBinding;

/// <summary>
/// Binds <see cref="PostMeldingBody.Metadata"/> from either bracketed form keys
/// (<c>Metadata[key]=value</c>, the default ASP.NET dictionary binding) or a single
/// <c>Metadata</c> form field containing a JSON object of string values, which is what
/// generated multipart clients such as hey-api send.
/// </summary>
internal sealed class PostMeldingMetadataModelBinder(IModelBinder defaultBinder) : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var request = bindingContext.HttpContext.Request;
        if (!request.HasFormContentType)
        {
            await defaultBinder.BindModelAsync(bindingContext);
            return;
        }

        var form = await request.ReadFormAsync(bindingContext.HttpContext.RequestAborted);
        var modelName = bindingContext.ModelName;

        if (!form.TryGetValue(modelName, out var jsonValues))
        {
            await defaultBinder.BindModelAsync(bindingContext);
            return;
        }

        if (form.Keys.Any(key => IsStructuredKey(key, modelName)))
        {
            Fail(
                bindingContext,
                $"Send {modelName} either as one JSON form field or as {modelName}[key] form fields, not both."
            );
            return;
        }

        if (jsonValues.Count != 1 || !TryParse(jsonValues[0], out var metadata))
        {
            Fail(
                bindingContext,
                $"{modelName} must be a single JSON object with string values, e.g. {{\"key\":\"value\"}}."
            );
            return;
        }

        bindingContext.Result = ModelBindingResult.Success(metadata);
    }

    private static bool IsStructuredKey(string key, string modelName) =>
        key.Length > modelName.Length
        && key.StartsWith(modelName, StringComparison.OrdinalIgnoreCase)
        && key[modelName.Length] is '[' or '.';

    private static bool TryParse(string? json, out Dictionary<string, string> metadata)
    {
        metadata = [];
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (
                    property.Value.ValueKind != JsonValueKind.String
                    || !metadata.TryAdd(property.Name, property.Value.GetString()!)
                )
                {
                    return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void Fail(ModelBindingContext bindingContext, string message)
    {
        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
        bindingContext.Result = ModelBindingResult.Failed();
    }
}

/// <summary>
/// Applies <see cref="PostMeldingMetadataModelBinder"/> to <see cref="PostMeldingBody.Metadata"/> only.
/// </summary>
internal sealed class PostMeldingMetadataModelBinderProvider : IModelBinderProvider
{
    private readonly DictionaryModelBinderProvider _dictionaryBinderProvider = new();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (
            context.Metadata.ContainerType != typeof(PostMeldingBody)
            || context.Metadata.PropertyName != nameof(PostMeldingBody.Metadata)
        )
        {
            return null;
        }

        var defaultBinder =
            _dictionaryBinderProvider.GetBinder(context)
            ?? throw new InvalidOperationException(
                $"No dictionary model binder for {nameof(PostMeldingBody)}.{nameof(PostMeldingBody.Metadata)}."
            );
        return new PostMeldingMetadataModelBinder(defaultBinder);
    }
}
