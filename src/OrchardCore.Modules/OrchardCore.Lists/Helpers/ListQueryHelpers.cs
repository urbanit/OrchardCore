using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Lists.Indexes;
using OrchardCore.Lists.Models;
using YesSql;

namespace OrchardCore.Lists.Helpers
{
    internal static class ListQueryHelpers
    {
        internal static Task<int> QueryListItemsCountAsync(ISession session, string listContentItemId, Expression<Func<ContentItemIndex, bool>> itemPredicate = null)
        {
            return session.Query<ContentItem>()
                    .With<ContainedPartIndex>(x => x.ListContentItemId == listContentItemId)
                    .With<ContentItemIndex>(itemPredicate ?? (x => x.Published))
                    .CountAsync();
        }

        internal static Task<IEnumerable<ContentItem>> QueryListItemsAsync(ISession session, string listContentItemId, Expression<Func<ContentItemIndex, bool>> itemPredicate = null, Expression<Func<ContainedPartIndex, object>> partPredicate = null)
        {
            return session.Query<ContentItem>()
                    .With<ContainedPartIndex>(x => x.ListContentItemId == listContentItemId)
                    .OrderBy(partPredicate ?? (c => c.ListContentItemId))
                    .With<ContentItemIndex>(itemPredicate ?? (x => x.Published))
                    .ListAsync();
        }
    }
}
