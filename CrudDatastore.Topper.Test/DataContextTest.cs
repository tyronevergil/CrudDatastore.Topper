﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CrudDatastore.Topper.Test
{
    [TestFixture()]
    public class DataContextTest
    {
        [Test()]
        public void CreateAction()
        {
            using (var context = DataContext.Factory())
            {
                var person = new Entities.Person
                {
                    Firstname = "Pauline",
                    Lastname = "Koch",
                    Identifications = new List<Entities.Identification>
                    {
                        new Entities.Identification
                        {
                            Type = Entities.Identification.Types.SSN,
                            Number = "222-222-2222"
                        }
                    }
                };

                context.Add(person);
                context.SaveChanges();

                Assert.IsTrue(person.PersonId > 0);
                Assert.IsTrue(context.Find(Specifications.PersonSpecs.Get(person.Firstname, person.Lastname)).Count() == 1);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.Firstname, person.Lastname)).Identifications.Count() == 1);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.Firstname, person.Lastname)).CreatedBy == "TestUser");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.Firstname, person.Lastname)).CreatedDate != DateTime.MinValue);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.Firstname, person.Lastname)).Identifications.FirstOrDefault().CreatedBy == "TestUser");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.Firstname, person.Lastname)).Identifications.FirstOrDefault().CreatedDate != DateTime.MinValue);
            }
        }

        [Test()]
        public void UpdateAction()
        {
            using (var context = DataContext.Factory())
            {
                var person = context.FindSingle(Specifications.PersonSpecs.Get(1));
                person.Firstname = "Rudolf";
                person.Identifications.Add(
                    new Entities.Identification
                    {
                        Type = Entities.Identification.Types.TIN,
                        Number = "333-333"
                    }
                );

                context.Update(person);
                context.SaveChanges();

                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Firstname == "Rudolf");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).LastModifiedBy == "TestUser");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).LastModifiedDate != DateTime.MinValue);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.Count == 3);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.LastOrDefault().CreatedBy == "TestUser");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.LastOrDefault().CreatedDate != DateTime.MinValue);
            }
        }

        [Test()]
        public void UpdateDeleteChildrenAction()
        {
            using (var context = DataContext.Factory())
            {
                var person = context.FindSingle(Specifications.PersonSpecs.Get(1));
                person.Firstname = "Rudolf";

                //var identifications = person.Identifications;

                person.Identifications = null;

                context.Update(person);
                context.SaveChanges();

                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Firstname == "Rudolf");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).LastModifiedBy == "TestUser");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).LastModifiedDate != DateTime.MinValue);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.Count == 0);
            }
        }

        [Test()]
        public void UpdateReplaceChildrenAction()
        {
            using (var context = DataContext.Factory())
            {
                var person = context.FindSingle(Specifications.PersonSpecs.Get(1));
                person.Firstname = "Rudolf";

                person.Identifications = new List<Entities.Identification>
                    {
                        new Entities.Identification
                        {
                            Type = Entities.Identification.Types.SSN,
                            Number = "444-222-2222"
                        }
                    };

                context.Update(person);
                context.SaveChanges();

                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Firstname == "Rudolf");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).LastModifiedBy == "TestUser");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).LastModifiedDate != DateTime.MinValue);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.Count == 1);
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.First().Number == "444-222-2222");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.First().CreatedBy == "TestUser");
                Assert.IsTrue(context.FindSingle(Specifications.PersonSpecs.Get(person.PersonId)).Identifications.First().CreatedDate != DateTime.MinValue);
            }
        }

        [Test()]
        public void DeleteAction()
        {
            using (var context = DataContext.Factory())
            {
                var person = context.FindSingle(Specifications.PersonSpecs.Get(1));

                context.Delete(person);
                context.SaveChanges();

                Assert.IsTrue(context.Find(Specifications.PersonSpecs.Get(person.PersonId)).Count() == 0);
                Assert.IsTrue(person.IsDeleted);
            }
        }

        [Test()]
        public void FindAction()
        {
            using (var context = DataContext.Factory())
            {
                var people = context.Find(Specifications.PersonSpecs.GetAll());

                Assert.IsTrue(people.Count() == 2);
            }
        }

        [Test()]
        public void FindNumberAction()
        {
            using (var context = DataContext.Factory())
            {
                var people = context.Find(Specifications.PersonSpecs.Get("92–4267"));

                Assert.IsTrue(people.Count() == 1);
            }
        }
    }
}
