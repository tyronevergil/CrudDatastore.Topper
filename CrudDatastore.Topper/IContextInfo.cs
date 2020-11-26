using System;
using System.Linq.Expressions;

namespace CrudDatastore.Topper
{
    public interface IContextInfo
    {
        string Username { get; }
        DateTime CurrentDate { get; }
    }

    public interface IContextFilterExpression : IContextInfo
    {
        Expression<Func<T, bool>> GetContextFilterExpression<T>();
    }
}
