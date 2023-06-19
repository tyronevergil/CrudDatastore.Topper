using System;
using System.Collections.Generic;
using System.Linq;
using CrudDatastore;
using Topper = CrudDatastore.Topper;

namespace CrudDatastore.Topper.Test
{
    public class UnitOfWorkInMemory : Topper.UnitOfWorkBase
    {
        public UnitOfWorkInMemory()
            : base(new Topper.ContextInfo("TestUser"))
        {
            var people = new List<Entities.Person>
            {
                new Entities.Person { PersonId = 1, Firstname = "Hermann", Lastname = "Einstein "},
                new Entities.Person { PersonId = 2, Firstname = "Albert", Lastname = "Einstein " },
                new Entities.Person { PersonId = 3, Firstname = "Maja", Lastname = "Einstein ", IsDeleted = true }
            };

            var identifications = new List<Entities.Identification>
            {
                new Entities.Identification { IdentificationId = 1, PersonId = 1, Type = Entities.Identification.Types.SSN, Number = "509–515-224" },
                new Entities.Identification { IdentificationId = 2, PersonId = 1, Type = Entities.Identification.Types.TIN, Number = "92–4267" },
                new Entities.Identification { IdentificationId = 3, PersonId = 2, Type = Entities.Identification.Types.SSN, Number = "425–428-336" },
                new Entities.Identification { IdentificationId = 4, PersonId = 1, Type = Entities.Identification.Types.SSN, Number = "323–442-336", IsDeleted = true },
            };

            var dataStorePerson = new DataStore<Entities.Person>(
                new DelegateCrudAdapter<Entities.Person>(this,
                    /* create */
                    (e) =>
                    {
                        var nextId = (people.Any() ? people.Max(p => p.PersonId) : 0) + 1;
                        e.PersonId = nextId;

                        people.Add(new Entities.Person
                        {
                            PersonId = e.PersonId,
                            Firstname = e.Firstname,
                            Lastname = e.Lastname,
                            CreatedBy = e.CreatedBy,
                            CreatedDate = e.CreatedDate
                        });
                    },

                    /* update */
                    (e) =>
                    {
                        var person = people.FirstOrDefault(p => p.PersonId == e.PersonId);
                        if (person != null)
                        {
                            person.Firstname = e.Firstname;
                            person.Lastname = e.Lastname;
                            person.LastModifiedBy = e.LastModifiedBy;
                            person.LastModifiedDate = e.LastModifiedDate;
                            person.IsDeleted = e.IsDeleted;
                        }
                    },

                    /* delete */
                    (e) =>
                    {
                        //var person = people.FirstOrDefault(p => p.PersonId == e.PersonId);
                        //if (person != null)
                        //{
                        //    people.Remove(person);
                        //}
                    },

                    /* read */
                    (predicate) =>
                    {
                        return people.Where(predicate.Compile()).AsQueryable();
                    }
                )
            );

            var dataStoreIdentification = new DataStore<Entities.Identification>(
                new DelegateCrudAdapter<Entities.Identification>(
                    /* create */
                    (e) =>
                    {
                        var nextId = (identifications.Any() ? identifications.Max(i => i.IdentificationId) : 0) + 1;
                        e.IdentificationId = nextId;

                        identifications.Add(new Entities.Identification
                        {
                            IdentificationId = e.IdentificationId,
                            PersonId = e.PersonId,
                            Type = e.Type,
                            Number = e.Number,
                            CreatedBy = e.CreatedBy,
                            CreatedDate = e.CreatedDate
                        });
                    },

                    /* update */
                    (e) =>
                    {
                        var identification = identifications.FirstOrDefault(i => i.IdentificationId == e.IdentificationId);
                        if (identification != null)
                        {
                            identification.PersonId = e.PersonId;
                            identification.Type = e.Type;
                            identification.Number = e.Number;
                            identification.LastModifiedBy = e.LastModifiedBy;
                            identification.LastModifiedDate = e.LastModifiedDate;
                            identification.IsDeleted = e.IsDeleted;
                        }
                    },

                    /* delete */
                    (e) =>
                    {
                        //var identification = identifications.FirstOrDefault(i => i.IdentificationId == e.IdentificationId);
                        //if (identification != null)
                        //{
                        //    identifications.Remove(identification);
                        //}
                    },

                    /* read */
                    (predicate) =>
                    {
                        return identifications.Where(predicate.Compile()).AsQueryable();
                    }
                )
            );

            this.Register(dataStorePerson)
                .Map(p => p.Identifications, (p, i) => p.PersonId == i.PersonId);
            this.Register(dataStoreIdentification);
        }
    }
}
