using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Deploy;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CoronaDeployments.Core.Models.Mvc
{
    public sealed class BuildTargetUpdateModel : IValidatableObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TargetRelativePath { get; set; }
        public BuildTargetType Type { get; set; }
        public DeployTargetType DeploymentType { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return new ValidationResult[0];
        }
    }
}
