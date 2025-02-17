﻿using HarshPoint.Provisioning;
using Microsoft.SharePoint.Client;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HarshPoint.Tests.Provisioning
{
    public class ContentTypeTests : SharePointClientTest
    {
        private String _guid;
        private HarshContentTypeId _id;

        private const String Group = "HarshPoint Unit Tests";

        public ContentTypeTests(ITestOutputHelper output) : base(output)
        {
            _guid = Guid.NewGuid().ToString("n");
            _id = HarshContentTypeId.Parse("0x01").Append(HarshContentTypeId.Parse(_guid));
        }

        [FactNeedsSharePoint]
        public async Task Existing_content_type_is_not_provisioned()
        {
            var prov = new HarshContentType()
            {
                Name = "Item",
                Id = HarshContentTypeId.Parse("0x01"),
            };

            await prov.ProvisionAsync(Context);

            var output = LastObjectOutput<ContentType>();
            Assert.False(output.ObjectAdded);
        }

        [FactNeedsSharePoint]
        public async Task ContentType_without_parent_gets_provisioned()
        {
            var prov = new HarshContentType()
            {
                Id = _id,
                Name = _guid,
                Description = _guid,
                Group = Group
            };

            ContentType ct = null;

            try
            {
                await prov.ProvisionAsync(Context);

                var output = LastObjectOutput<ContentType>();
                ct = output.Object;
                Assert.True(output.ObjectAdded);
                Assert.NotNull(ct);

                ClientContext.Load(
                    ct,
                    c => c.Name,
                    c => c.Description,
                    c => c.Group,
                    c => c.StringId
                );

                await ClientContext.ExecuteQueryAsync();

                Assert.Equal(_guid, ct.Name);
                Assert.Equal(_guid, ct.Description);
                Assert.Equal(Group, ct.Group);
                Assert.Equal(_id.ToString(), ct.StringId);
            }
            finally
            {
                if (ct != null)
                {
                    ct.DeleteObject();
                    await ClientContext.ExecuteQueryAsync();
                }
            }
        }

        [FactNeedsSharePoint]
        public async Task Child_fieldref_get_added()
        {
            var fieldId = Guid.NewGuid();

            var field = new HarshField()
            {
                Id = fieldId,
                InternalName = fieldId.ToString("n"),
                Type = FieldType.Text,
            };

            var ct = new HarshContentType()
            {
                Id = _id,
                Name = _guid,
                Description = _guid,
                Group = Group,
                Children =
                {
                    new HarshFieldRef()
                    {
                        Fields = Resolve.Field().ById(fieldId),
                    },
                }
            };

            try
            {
                await field.ProvisionAsync(Context);
                await ct.ProvisionAsync(Context);

                var cto = LastObjectOutput<ContentType>();

                Assert.True(cto.ObjectAdded);
                Assert.False(cto.Object.IsNull());

                var links = ClientContext.LoadQuery(
                    cto.Object.FieldLinks
                    .Where(fl => fl.Id == fieldId)
                    .Include(
                        fl => fl.Name,
                        fl => fl.Id
                    )
                );

                await ClientContext.ExecuteQueryAsync();

                var link = Assert.Single(links);

                Assert.NotNull(link);
                Assert.Equal(fieldId, link.Id);
                Assert.Equal(field.InternalName, link.Name);
            }
            finally
            {
                try
                {
                    Web.ContentTypes.GetById(_id.ToString()).DeleteObject();
                }
                finally
                {
                    Web.Fields.GetById(fieldId).DeleteObject();
                }
            }
        }

        [FactNeedsSharePoint]
        public async Task Default_group_is_used()
        {
            var str = Guid.NewGuid().ToStringInvariant("n");
            var prov = new HarshContentType()
            {
                Id = HarshContentTypeId.Parse($"0x0100{str}"),
                Name = str,
            };

            ContentType ct = null;
            try
            {
                var ctx = Context.PushState(new DefaultContentTypeGroup()
                {
                    Value = Group
                });

                await prov.ProvisionAsync(ctx);

                var cto = LastObjectOutput<ContentType>();
                RegisterForDeletion(cto.Object);

                Assert.True(cto.ObjectAdded);

                ct = cto.Object;
                Assert.NotNull(ct);
                Assert.Equal(Group, ct.Group);
            }
            finally
            {
                ct?.DeleteObject();
                await ClientContext.ExecuteQueryAsync();
            }
        }
    }
}
