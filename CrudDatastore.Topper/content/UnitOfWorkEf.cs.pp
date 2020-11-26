using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Linq.Expressions;
using CrudDatastore;
using Topper = CrudDatastore.Topper;

namespace $rootnamespace$
{
    public class UnitOfWorkEf : UnitOfWorkEfBase
    {
        public UnitOfWorkEf()
            :base("/* connection string here */", new Topper.ContextInfo("/* username here */"))
        { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entities.Person>()
                .HasMany(p => p.Identifications)
                .WithOptional()
                .HasForeignKey(i => i.PersonId);

            modelBuilder.Entity<Entities.Person>()
                .ToTable("People");

            modelBuilder.Entity<Entities.Identification>()
                .ToTable("Identifications");

            base.OnModelCreating(modelBuilder);
        }
    }

    public class UnitOfWorkEfBase : DbContext, Topper.IUnitOfWork
    {
        private readonly Topper.IContextFilterExpression _context;
        private readonly IDictionary<Type, object> _dataQueries = new Dictionary<Type, object>();

        public event EventHandler<EntityEventArgs> EntityMaterialized;
        public event EventHandler<EntityEventArgs> EntityCreate;
        public event EventHandler<EntityEventArgs> EntityUpdate;
        public event EventHandler<EntityEventArgs> EntityDelete;

        public UnitOfWorkEfBase(string connectionString, Topper.IContextFilterExpression context)
            : base(connectionString)
        {
            _context = context;

            Database.SetInitializer<UnitOfWorkEfBase>(null);
            DbInterception.Add(new CommandTreeInterceptor(_context));
            ((IObjectContextAdapter)this).ObjectContext.ObjectMaterialized += (sender, e) => EntityMaterialized?.Invoke(this, new EntityEventArgs(e.Entity));
        }

        public string Username
        {
            get { return _context.Username; }
        }

        public DateTime CurrentDate
        {
            get { return _context.CurrentDate; }
        }
        
        public void Execute(string command, params object[] parameters)
        {
            Database.ExecuteSqlCommand(command, parameters);
        }

        public IDataQuery<T> Read<T>() where T : CrudDatastore.EntityBase
        {
            var entityType = typeof(T);
            if (_dataQueries.ContainsKey(entityType))
                return (IDataQuery<T>)_dataQueries[entityType];

            var dataQuery = new DataQuery<T>(new DbSetQueryAdapter<T>(this));
            _dataQueries.Add(entityType, dataQuery);

            return dataQuery;
        }

        public void MarkNew<T>(T entity) where T : CrudDatastore.EntityBase
        {
            Set<T>().Add(entity);
        }

        public void MarkModified<T>(T entity) where T : CrudDatastore.EntityBase
        {
            var entry = Entry(entity);
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged || entry.State == EntityState.Deleted)
                entry.State = EntityState.Modified;
        }

        public void MarkDeleted<T>(T entity) where T : CrudDatastore.EntityBase
        {
            var entry = Entry(entity);
            if (entry.State == EntityState.Detached)
                Set<T>().Attach(entity);

            Set<T>().Remove(entity);
        }

        public void Commit()
        {
            ChangeTracker.DetectChanges();

            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        EntityCreate?.Invoke(this, new EntityEventArgs(entry.Entity));
                        break;
                    case EntityState.Modified:
                        EntityUpdate?.Invoke(this, new EntityEventArgs(entry.Entity));
                        break;
                    case EntityState.Deleted:
                        EntityDelete?.Invoke(this, new EntityEventArgs(entry.Entity));
                        break;
                }
            }

            SaveChanges();
        }
    }
    
    internal class DbSetQueryAdapter<T> : DelegateQueryAdapter<T> where T : EntityBase
    {
        public DbSetQueryAdapter(DbContext dbContext)
            : base
            (
                /* read */
                (predicate) =>
                {
                    return dbContext.Set<T>().Where(predicate);
                },

                /* read - command */
                (command, parameters) =>
                {
                    return dbContext.Database.SqlQuery<T>(command, parameters).AsQueryable();
                }
            )
        { }
    }

    internal class CommandTreeInterceptor : IDbCommandTreeInterceptor
    {
        private readonly Topper.IContextFilterExpression _context;

        public CommandTreeInterceptor(Topper.IContextFilterExpression context)
        {
            _context = context;
        }

        public void TreeCreated(DbCommandTreeInterceptionContext interceptionContext)
        {
            if (interceptionContext.Result.CommandTreeKind == DbCommandTreeKind.Query)
                InterceptQuery(interceptionContext);
        }

        private void InterceptQuery(DbCommandTreeInterceptionContext interceptionContext)
        {
            if (interceptionContext.Result.DataSpace != DataSpace.SSpace)
                return;

            var query = interceptionContext.Result as DbQueryCommandTree;
            if (query != null)
            {
                var modified = query.Query.Accept(new DataContextExpressionTreeModifier(_context));
                interceptionContext.Result = new DbQueryCommandTree(query.MetadataWorkspace, query.DataSpace, modified);
            }
        }

        private class DataContextExpressionTreeModifier : DefaultExpressionVisitor
        {
            private readonly Topper.IContextFilterExpression _context;
            private static readonly IEnumerable<Type> _entityTypes;

            private readonly IList<DbScanExpression> _scanExpressions;
            private static readonly IList<DbFilterExpression> _filterExpressions;

            static DataContextExpressionTreeModifier()
            {
                _entityTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsSubclassOf(typeof(Topper.EntityBase)) && !t.IsAbstract)
                    .ToList();

                _filterExpressions = new List<DbFilterExpression>();
            }

            public DataContextExpressionTreeModifier(Topper.IContextFilterExpression context)
            {
                _context = context;

                _scanExpressions = new List<DbScanExpression>();
            }

            public override DbExpression Visit(DbScanExpression expression)
            {
                var baseResult = base.Visit(expression);

                var table = expression.Target.ElementType as EntityType;
                if (table != null)
                {
                    var entityType = _entityTypes.FirstOrDefault(t => t.Name == table.Name);
                    if (entityType != null)
                    {
                        if (_scanExpressions.Contains(expression))
                        {
                            return baseResult;
                        }

                        var contextLamdaExpression = typeof(Topper.IContextFilterExpression).GetMethod("GetContextFilterExpression").MakeGenericMethod(new[] { entityType }).Invoke(_context, null) as LambdaExpression;
                        if (contextLamdaExpression != null)
                        {
                            var binding = baseResult.Bind();
                            var contextFilter = LambdaToDbExpressionVisitor.Convert(binding, contextLamdaExpression);

                            var filterExpression = binding.Filter(contextFilter);

                            lock (_filterExpressions)
                            {
                                if (!_filterExpressions.Any(f => ((DbScanExpression)f.Input.Expression).Target.Name == table.Name))
                                {
                                    _filterExpressions.Add(filterExpression);
                                }
                            }

                            return filterExpression;
                        }
                    }
                }

                return baseResult;
            }

            public override DbExpression Visit(DbFilterExpression expression)
            {
                if (expression.Input.Expression is DbScanExpression scanExpression)
                {
                    if (HasFilterExpression(expression.Predicate, scanExpression.Target.Name))
                    {
                        _scanExpressions.Add(scanExpression);
                    }
                }

                return base.Visit(expression);
            }

            private bool HasFilterExpression(DbExpression predicate, string target)
            {
                if (predicate is DbAndExpression andExpression)
                {
                    return HasFilterExpression(andExpression.Left, target) || HasFilterExpression(andExpression.Right, target);
                }

                var comparison = predicate as DbComparisonExpression;
                if (comparison != null)
                {
                    lock (_filterExpressions)
                    {
                        var filters = _filterExpressions.Where(f => ((DbScanExpression)f.Input.Expression).Target.Name == target);
                        if (filters.Any())
                        {
                            var leftPropertyComparison = comparison.Left as DbPropertyExpression;
                            if (leftPropertyComparison != null)
                            {
                                var filter = filters.First().Predicate as DbComparisonExpression;
                                if (((DbPropertyExpression)filter.Left).Property == leftPropertyComparison.Property)
                                {
                                    return true;
                                }
                            }

                            var rightPropertyComparison = comparison.Right as DbPropertyExpression;
                            if (rightPropertyComparison != null)
                            {
                                var filter = filters.First().Predicate as DbComparisonExpression;
                                if (((DbPropertyExpression)filter.Right).Property == rightPropertyComparison.Property)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }
        }

        private class LambdaToDbExpressionVisitor : ExpressionVisitor
        {
            private Dictionary<Expression, DbExpression> _expressionMap = new Dictionary<Expression, DbExpression>();
            private DbExpressionBinding _binding;
            private Expression _expression;

            public static DbExpression Convert(DbExpressionBinding binding, LambdaExpression expression)
            {
                var visitor = new LambdaToDbExpressionVisitor(binding);
                visitor.Visit(expression.Body);
                return visitor.GetConvertedDbExpression();
            }

            private LambdaToDbExpressionVisitor(DbExpressionBinding binding)
            {
                _binding = binding;
            }

            private DbExpression GetConvertedDbExpression()
            {
                return _expressionMap[_expression];
            }

            public override Expression Visit(Expression node)
            {
                if (_expression == null)
                    _expression = node;

                return base.Visit(node);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var expression = base.VisitBinary(node) as BinaryExpression;

                // check mapping here
                DbExpression leftExpression = _expressionMap[expression.Left]; 
                DbExpression rightExpression = _expressionMap[expression.Right]; 

                var dbExpression = default(DbExpression);
                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        dbExpression = DbExpressionBuilder.Equal(leftExpression, rightExpression);
                        break;
                    case ExpressionType.NotEqual:
                        dbExpression = DbExpressionBuilder.NotEqual(leftExpression, rightExpression);
                        break;
                    case ExpressionType.GreaterThan:
                        dbExpression = DbExpressionBuilder.GreaterThan(leftExpression, rightExpression);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        dbExpression = DbExpressionBuilder.GreaterThanOrEqual(leftExpression, rightExpression);
                        break;
                    case ExpressionType.LessThan:
                        dbExpression = DbExpressionBuilder.LessThan(leftExpression, rightExpression);
                        break;
                    case ExpressionType.LessThanOrEqual:
                        dbExpression = DbExpressionBuilder.LessThanOrEqual(leftExpression, rightExpression);
                        break;
                    case ExpressionType.And:
                        dbExpression = DbExpressionBuilder.And(leftExpression, rightExpression);
                        break;
                    case ExpressionType.Or:
                        dbExpression = DbExpressionBuilder.Or(leftExpression, rightExpression);
                        break;

                    /* default - more work todo here! */
                }

                if (dbExpression != null)
                    _expressionMap[expression] = dbExpression;

                return expression;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                var expression = base.VisitConstant(node) as ConstantExpression;
                var type = node.Type;

                if (type == typeof(byte))
                    _expressionMap[expression] = DbExpression.FromByte((byte?)node.Value);
                else if (type == typeof(bool))
                    _expressionMap[expression] = DbExpression.FromBoolean((bool?)node.Value);
                else if (type == typeof(DateTime))
                    _expressionMap[expression] = DbExpression.FromDateTime((DateTime?)node.Value);
                else if (type == typeof(DateTimeOffset))
                    _expressionMap[expression] = DbExpression.FromDateTimeOffset((DateTimeOffset?)node.Value);
                else if (type == typeof(decimal))
                    _expressionMap[expression] = DbExpression.FromDecimal((decimal?)node.Value);
                else if (type == typeof(double))
                    _expressionMap[expression] = DbExpression.FromDouble((double?)node.Value);
                else if (type == typeof(Guid))
                    _expressionMap[expression] = DbExpression.FromGuid((Guid?)node.Value);
                else if (type == typeof(Int16))
                    _expressionMap[expression] = DbExpression.FromInt16((Int16?)node.Value);
                else if (type == typeof(Int32))
                    _expressionMap[expression] = DbExpression.FromInt32((Int32?)node.Value);
                else if (type == typeof(Int64))
                    _expressionMap[expression] = DbExpression.FromInt64((Int64?)node.Value);
                else if (type == typeof(float))
                    _expressionMap[expression] = DbExpression.FromSingle((float?)node.Value);
                else if (type == typeof(string))
                    _expressionMap[expression] = DbExpression.FromString((string)node.Value);
                /* else - more work todo here! */

                return expression;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var expression = base.VisitMember(node) as MemberExpression;
                var dbExpression = _binding.VariableType.Variable(_binding.VariableName).Property(expression.Member.Name);

                _expressionMap[expression] = dbExpression;

                return expression;
            }
        }
    }
}
