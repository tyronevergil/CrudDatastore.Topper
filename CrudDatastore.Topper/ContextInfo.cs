using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CrudDatastore.Topper
{
    public class ContextInfo : IContextInfo, IContextFilterExpression
    {
        private readonly string _username;
        private readonly Func<DateTime> _currentDateFactory;

        public ContextInfo(string username)
            : this(username, () => DateTime.Now)
        { }

        public ContextInfo(string username, Func<DateTime> currentDateFactory)
        {
            _username = username;
            _currentDateFactory = currentDateFactory;
        }

        public string Username
        {
            get { return _username; }
        }

        public DateTime CurrentDate
        {
            get { return _currentDateFactory();  }
        }

        public Expression<Func<T, bool>> GetContextFilterExpression<T>()
        {
            var typeParam = Expression.Parameter(typeof(T), "b");
            var expression = default(Expression);

            foreach (var buildExpr in GetExpressionBuilders<T>())
            {
                var expr = buildExpr(typeParam);
                if (expr != null)
                {
                    if (expression != null)
                        expression = Expression.And(expression, expr);
                    else
                        expression = expr;
                }
            }

            if (expression != null)
            {
                return Expression.Lambda<Func<T, bool>>(expression, typeParam);
            }
            else
            {
                return null;
            }
        }

        protected virtual IEnumerable<Func<ParameterExpression, Expression>> GetExpressionBuilders<T>() 
        {
            return new Func<ParameterExpression, Expression>[] { DeletedExpressionBuilder<T> };
        }

        private Expression DeletedExpressionBuilder<T>(ParameterExpression typeParam)
        {
            if (typeof(IDeletedEntity).IsAssignableFrom(typeof(T)))
            {
                var deletedParam = Expression.Constant(false, typeof(bool));
                var typeParamProp = Expression.Property(typeParam, "IsDeleted");

                //"b.IsDeleted = <constant deleted>"
                var expression = Expression.Equal(typeParamProp, deletedParam);

                return expression;
            }

            return null;
        }
    }
}
