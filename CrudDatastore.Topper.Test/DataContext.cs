using System;
using Topper = CrudDatastore.Topper;

namespace CrudDatastore.Topper.Test
{
    public class DataContext : Topper.DataContextBase
    {
        private DataContext(Topper.IUnitOfWork unitOfWork)
            : base(unitOfWork)
        {
        }

        public static DataContext Factory()
        {
            return new DataContext(new UnitOfWorkInMemory());
        }
    }
}
