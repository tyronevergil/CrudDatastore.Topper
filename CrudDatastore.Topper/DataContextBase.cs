using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CrudDatastore;

namespace CrudDatastore.Topper
{
    public class DataContextBase : CrudDatastore.DataContextBase
    {
        private readonly IContextInfo _contextInfo;

        public DataContextBase(IUnitOfWork unitOfWork)
            : base(unitOfWork)
        {
            _contextInfo = unitOfWork;
        }

        protected  override void OnEntityCreate(object entity)
        {
            (entity as ICreatedEntity)
                .Use(entityCast =>
                {
                    entityCast.CreatedBy = _contextInfo.Username;
                    entityCast.CreatedDate = _contextInfo.CurrentDate;
                });

            UpdateLastModifiedEntity(entity);
        }

        protected override void OnEntityUpdate(object entity)
        {
            UpdateLastModifiedEntity(entity);
        }

        protected override void OnEntityDelete(object entity)
        {
            (entity as IDeletedEntity)
                .Use(entityCast =>
                {
                    Entry(entity).MarkModified();

                    entityCast.IsDeleted = true;
                });

            UpdateLastModifiedEntity(entity);
        }

        private void UpdateLastModifiedEntity(object entity)
        {
            (entity as ILastModifiedEntity)
                .Use(entityCast =>
                {
                    entityCast.LastModifiedBy = _contextInfo.Username;
                    entityCast.LastModifiedDate = _contextInfo.CurrentDate;
                });
        }
    }
}
