using System;
using System.Linq.Expressions;

namespace Genius.PriceChecker.UI.Forms.Helpers
{
    internal static class ExpressionHelpers
    {
        public static string GetPropertyName<TContainer, TProp>(Expression<Func<TContainer, TProp>> propertyLambda)
        {
            MemberExpression body = propertyLambda.Body as MemberExpression;

            if (body == null) {
                UnaryExpression ubody = (UnaryExpression)propertyLambda.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}
