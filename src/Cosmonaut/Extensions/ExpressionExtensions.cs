using System;
using System.Linq.Expressions;
using Cosmonaut.Configuration;

namespace Cosmonaut.Extensions
{
    internal static class ExpressionExtensions
    {
        internal static Expression<Func<TEntity, bool>> SharedCollectionExpression<TEntity>(this EntityCollectionMapping entityCollectionMapping) where TEntity : class
        {
            var parameter = Expression.Parameter(typeof(ISharedCosmosEntity));
            var member = Expression.Property(parameter, nameof(ISharedCosmosEntity.CosmosEntityName));
            var constant = Expression.Constant(entityCollectionMapping.CollectionName);
            var body = Expression.Equal(member, constant);
            var extra = Expression.Lambda<Func<TEntity, bool>>(body, parameter);
            return extra;
        }
    }
}