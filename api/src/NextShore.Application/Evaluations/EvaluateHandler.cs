using FluentValidation;
using NextShore.Application.Engine;
using NextShore.Application.Profiles;
using NextShore.Application.Results;
using NextShore.Domain;

namespace NextShore.Application.Evaluations;

/// <summary><c>POST /evaluations</c>: what-if run of the engine. Not persisted, no AI.</summary>
public sealed class EvaluateHandler(IValidator<CandidateProfileRequest> validator, EngineRunner engine)
{
    public async Task<Result<EvaluationResponse>> HandleAsync(CandidateProfileRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.ToDictionary().AsReadOnly());
        }

        var (result, snapshot) = await engine.RunAsync(CandidateProfileMapper.ToDomain(request), cancellationToken);
        return EvaluationResponseMapper.From(result, snapshot);
    }
}
