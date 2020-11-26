using System;

namespace CrudDatastore.Topper
{
    public class UnitOfWorkBase : CrudDatastore.UnitOfWorkBase, IUnitOfWork
    {
        private readonly IContextFilterExpression _context;

        public UnitOfWorkBase(IContextFilterExpression context)
        {
            _context = context;
        }

        public string Username
        {
            get { return _context.Username; }
        }

        public DateTime CurrentDate
        {
            get { return _context.CurrentDate; }
        }

        protected override IPropertyMap<T> Register<T>(IDataStore<T> dataStore)
        {
            var ds = new DataStore<T>((ICrud<T>)dataStore, _context);
            return base.Register(ds);
        }
    }
}
