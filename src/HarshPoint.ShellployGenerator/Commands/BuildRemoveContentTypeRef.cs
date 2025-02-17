﻿using HarshPoint.Provisioning;
using HarshPoint.ShellployGenerator.Builders;
using System.CodeDom;

namespace HarshPoint.ShellployGenerator.Commands
{
    internal sealed class BuildRemoveContentTypeRef :
        NewProvisionerCommandBuilder<HarshRemoveContentTypeRef>
    {
        public BuildRemoveContentTypeRef()
        {
            ProvisionerDefaults.Include(this);

            PositionalParameter("ContentTypeId").SynthesizeMandatory(
                typeof(HarshContentTypeId[])
            );

            Parameter(x => x.ContentTypes).SetFixedValue(
                new CodeTypeReferenceExpression(typeof(Resolve))
                    .Call(nameof(Resolve.ContentType))
                    .Call(nameof(Resolve.ById), new CodeVariableReferenceExpression("ContentTypeId"))
            );
        }
    }
}