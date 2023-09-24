using CoronaDeployments.Core.Deploy;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoronaDeployments.Core.Models.Mvc
{
    public class IISBuildTargetConfigurationCreateModel
    {
        public Guid ProjectId { get; set; }

        public Guid BuildTargetId { get; set; }

        public IISDeployTargetExtraInfo Configuration { get; set; }
        
        public Guid CreatedByUserId { get; set; }
    }
}
