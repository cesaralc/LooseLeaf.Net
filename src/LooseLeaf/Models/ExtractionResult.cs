using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace LooseLeaf.Models;

public sealed class ExtractionResult<T>
{
    private readonly IReadOnlyDictionary<string, FieldSource> _sources;

    public ExtractionResult(
        T data,
        IReadOnlyDictionary<string, FieldSource> sources,
        IReadOnlyList<ValidationIssue> validationIssues,
        ProcessedDocument processedDocument)
    {
        Data = data;
        _sources = sources;
        ValidationIssues = validationIssues;
        ProcessedDocument = processedDocument;
    }

    public T Data { get; }

    public IReadOnlyList<ValidationIssue> ValidationIssues { get; }

    public ProcessedDocument ProcessedDocument { get; }

    public IReadOnlyCollection<FieldSource> Citations => _sources.Values.ToList();

    public FieldSource? GetSource(Expression<Func<T, object?>> selector)
    {
        var key = PropertyPath.From(selector);
        return _sources.TryGetValue(key, out var source) ? source : null;
    }

    public static IReadOnlyList<ValidationIssue> ValidateData(T data)
    {
        var context = new ValidationContext(data!);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        Validator.TryValidateObject(data!, context, results, validateAllProperties: true);
        return results.Select(r => new ValidationIssue
        {
            Field = r.MemberNames.FirstOrDefault() ?? string.Empty,
            Message = r.ErrorMessage ?? "Validation failed."
        }).ToList();
    }
}

internal static class PropertyPath
{
    public static string From<T>(Expression<Func<T, object?>> selector)
    {
        var expression = selector.Body;
        if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
        {
            expression = unary.Operand;
        }

        var parts = new Stack<string>();
        while (expression is MemberExpression memberExpression)
        {
            parts.Push(memberExpression.Member.Name);
            expression = memberExpression.Expression!;
        }

        return string.Join('.', parts);
    }
}
