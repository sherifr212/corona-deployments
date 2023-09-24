using CoronaDeployments.Core;
using CoronaDeployments.Core.Build;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaDeployments.Core.Models.Mvc
{
    public sealed class ProjectCreateModel : IValidatableObject
    {
        public string Name { get; set; }
        public string RepositoryUrl { get; set; }
        public string BranchName { get; set; }
        public SourceCodeRepositoryType RepositoryType { get; set; }
        public Guid CreatedByUserId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new ValidationResult[0];
        }
    }
}
