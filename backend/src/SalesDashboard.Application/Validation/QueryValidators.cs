using FluentValidation;
using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Periods;

namespace SalesDashboard.Application.Validation;

/// <summary>Rules for the period part shared by all queries. Concrete validators inherit it.</summary>
public abstract class PeriodQueryValidatorBase<T> : AbstractValidator<T>
    where T : PeriodQuery
{
    public static readonly DateOnly EarliestDate = new(2000, 1, 1);
    public const int MaxCustomRangeDays = 1830; // ~5 years

    protected PeriodQueryValidatorBase()
    {
        RuleFor(q => q.Preset).IsInEnum();

        When(q => q.Preset == PeriodPreset.Custom, () =>
        {
            RuleFor(q => q.From).NotNull().WithMessage("'from' is required when preset is Custom.");
            RuleFor(q => q.To).NotNull().WithMessage("'to' is required when preset is Custom.");

            When(q => q.From.HasValue && q.To.HasValue, () =>
            {
                RuleFor(q => q.From!.Value)
                    .GreaterThanOrEqualTo(EarliestDate)
                    .WithName("from");

                RuleFor(q => q.To!.Value)
                    .GreaterThanOrEqualTo(q => q.From!.Value)
                    .WithName("to")
                    .WithMessage("'to' must not be earlier than 'from'.");

                RuleFor(q => q)
                    .Must(q => q.To!.Value.DayNumber - q.From!.Value.DayNumber + 1 <= MaxCustomRangeDays)
                    .When(q => q.To >= q.From)
                    .WithName("to")
                    .WithMessage($"The range must not exceed {MaxCustomRangeDays} days.");
            });
        }).Otherwise(() =>
        {
            RuleFor(q => q.From).Null().WithMessage("'from' is only allowed when preset is Custom.");
            RuleFor(q => q.To).Null().WithMessage("'to' is only allowed when preset is Custom.");
        });
    }
}

public sealed class PeriodQueryValidator : PeriodQueryValidatorBase<PeriodQuery>;

public sealed class RankingQueryValidator : PeriodQueryValidatorBase<RankingQuery>
{
    public RankingQueryValidator() => RuleFor(q => q.RankBy).IsInEnum();
}

public sealed class TrendQueryValidator : PeriodQueryValidatorBase<TrendQuery>
{
    public TrendQueryValidator() => RuleFor(q => q.Granularity).IsInEnum().When(q => q.Granularity.HasValue);
}

public sealed class TopProductsQueryValidator : PeriodQueryValidatorBase<TopProductsQuery>
{
    public const int MaxLimit = 50;

    public TopProductsQueryValidator()
    {
        RuleFor(q => q.Limit).InclusiveBetween(1, MaxLimit);
        RuleFor(q => q.SortBy).IsInEnum();
    }
}

public sealed class RecentSalesQueryValidator : PeriodQueryValidatorBase<RecentSalesQuery>
{
    public const int MaxPageSize = 100;

    public RecentSalesQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue);
    }
}
