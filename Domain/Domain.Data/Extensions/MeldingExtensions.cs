using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public static class MeldingExtensions
{
    public static void AddTag(this Melding melding, string key, string value)
    {
        var tags = melding.Tags;
        tags[key] = value;
    }

    public static void AddTag<T, TValue>(
        this Melding melding,
        Expression<Func<T, TValue>> expression,
        string value
    )
    {
        var memberInfo = expression.GetMemberInfo(melding);
        AddTag(melding, JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name), value);
    }

    public static string GetRequiredTag(this Melding melding, string tagName)
    {
        if (melding.Tags.TryGetValue(tagName, out var tag))
        {
            return tag;
        }
        else
        {
            throw new TagNotFoundException(tagName, melding.Id);
        }
    }

    public static string? GetTag(this Melding melding, string tagName)
    {
        try
        {
            return GetRequiredTag(melding, tagName);
        }
        catch (Exception) { }
        return null;
    }

    public static string GetRequiredTag<T, TValue>(
        this Melding melding,
        Expression<Func<T, TValue>> expression
    )
    {
        var memberInfo = expression.GetMemberInfo(melding);
        var tagName = JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name);
        return GetRequiredTag(melding, tagName);
    }

    public static string? GetTag<T, TValue>(
        this Melding melding,
        Expression<Func<T, TValue>> expression
    )
    {
        try
        {
            return GetRequiredTag(melding, expression);
        }
        catch (Exception) { }
        return null;
    }

    public static void AddInternalTag(this Melding melding, string key, string value)
    {
        var tags = melding.InternalTags;
        tags[key] = value;
    }

    public static void AddInternalTag<T, TValue>(
        this Melding melding,
        Expression<Func<T, TValue>> expression,
        string value
    )
    {
        var memberInfo = expression.GetMemberInfo(melding);
        AddInternalTag(melding, JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name), value);
    }

    public static string GetRequiredInternalTag(this Melding melding, string tagName)
    {
        if (melding.InternalTags.TryGetValue(tagName, out var tag))
        {
            return tag;
        }
        else
        {
            throw new TagNotFoundException(tagName, melding.Id);
        }
    }

    public static string? GetInternalTag(this Melding melding, string tagName)
    {
        try
        {
            return GetRequiredInternalTag(melding, tagName);
        }
        catch (Exception) { }
        return null;
    }

    public static string GetRequiredInternalTag<T, TValue>(
        this Melding melding,
        Expression<Func<T, TValue>> expression,
        bool throwExceptionIfNotFound = true
    )
    {
        var memberInfo = expression.GetMemberInfo(melding);
        var tagName = JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name);
        return GetRequiredInternalTag(melding, tagName);
    }

    public static string? GetInternalTag<T, TValue>(
        this Melding melding,
        Expression<Func<T, TValue>> expression
    )
    {
        try
        {
            return GetRequiredInternalTag(melding, expression);
        }
        catch (Exception) { }
        return null;
    }

    private static MemberInfo GetMemberInfo<T, TValue>(
        this Expression<Func<T, TValue>> expression,
        Melding melding
    )
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            if (memberExpression.Member is MemberInfo memberInfo)
            {
                return memberInfo;
            }
        }
        throw new TagNotFoundException("could-not-get-tag-id", melding.Id);
    }
}
