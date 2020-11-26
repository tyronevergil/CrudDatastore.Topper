using System;
using System.Linq;
using System.Linq.Expressions;

namespace CrudDatastore.Topper
{
    internal class DataStore<T> : CrudDatastore.DataStore<T>, ICrud<T>, IQuery<T> where T : CrudDatastore.EntityBase
    {
        private readonly IContextFilterExpression _context;
        private readonly ICrud<T> _crud;

        public DataStore(ICrud<T> crud, IContextFilterExpression context)
            : base(crud)
        {
            _context = context;
            _crud = crud;
        }

        IQuery<T> ICrud<T>.Read()
        {
            return this;
        }

        IQueryable<T> IQuery<T>.Execute(Expression<Func<T, bool>> predicate)
        {
            var modifiedPredicate = predicate;

            var filterExpression = _context.GetContextFilterExpression<T>();
            if (filterExpression != null)
            {
                var typeParam = predicate.Parameters[0];
                var filter = new ParameterReplacerVisitor(typeParam).Visit(filterExpression.Body);

                modifiedPredicate = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(predicate.Body, filter), typeParam);
            }

            return ((IQuery<T>)_crud).Execute(modifiedPredicate);
        }

        IQueryable<T> IQuery<T>.Execute(string command, params object[] parameters)
        {
            var filterExpression = _context.GetContextFilterExpression<T>();
            if (filterExpression != null)
            {
                return ((IQuery<T>)_crud).Execute(command, parameters).Where(filterExpression);
            }

            return ((IQuery<T>)_crud).Execute(command, parameters);
        }

        private class ParameterReplacerVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;

            public ParameterReplacerVisitor(ParameterExpression parameter)
            {
                _parameter = parameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type.IsAssignableFrom(_parameter.Type))
                    return _parameter;

                return base.VisitParameter(node);
            }
        }
    }
}
