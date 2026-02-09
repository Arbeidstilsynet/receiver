using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public static class MeldingExtensions
{
    public static bool ContainsDocument(this Melding? melding, Guid documentId)
    {
        return melding != null
            && (
                melding.MainContentId == documentId
                || melding.StructuredDataId == documentId
                || melding.AttachmentIds.Contains(documentId)
            );
    }

    extension(Melding melding)
    {
        public void AddTag(string key, string value)
        {
            var tags = melding.Tags;
            tags[key] = value;
        }

        public void AddTag<T, TValue>(Expression<Func<T, TValue>> expression, string value)
        {
            var memberInfo = expression.GetMemberInfo(melding);
            AddTag(melding, JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name), value);
        }

        public string GetRequiredTag(string tagName)
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

        public string? GetTag(string tagName)
        {
            try
            {
                return GetRequiredTag(melding, tagName);
            }
            catch (Exception) { }
            return null;
        }

        public string GetRequiredTag<T, TValue>(Expression<Func<T, TValue>> expression)
        {
            var memberInfo = expression.GetMemberInfo(melding);
            var tagName = JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name);
            return GetRequiredTag(melding, tagName);
        }

        public string? GetTag<T, TValue>(Expression<Func<T, TValue>> expression)
        {
            try
            {
                return GetRequiredTag(melding, expression);
            }
            catch (Exception) { }
            return null;
        }

        public void AddInternalTag(string key, string value)
        {
            var tags = melding.InternalTags;
            tags[key] = value;
        }

        public void AddInternalTag<T, TValue>(Expression<Func<T, TValue>> expression, string value)
        {
            var memberInfo = expression.GetMemberInfo(melding);
            AddInternalTag(melding, JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name), value);
        }

        public string GetRequiredInternalTag(string tagName)
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

        public string? GetInternalTag(string tagName)
        {
            try
            {
                return GetRequiredInternalTag(melding, tagName);
            }
            catch (Exception) { }
            return null;
        }

        public string GetRequiredInternalTag<T, TValue>(
            Expression<Func<T, TValue>> expression,
            bool throwExceptionIfNotFound = true
        )
        {
            var memberInfo = expression.GetMemberInfo(melding);
            var tagName = JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name);
            return GetRequiredInternalTag(melding, tagName);
        }

        public string? GetInternalTag<T, TValue>(Expression<Func<T, TValue>> expression)
        {
            try
            {
                return GetRequiredInternalTag(melding, expression);
            }
            catch (Exception) { }
            return null;
        }
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
