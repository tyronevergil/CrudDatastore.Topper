﻿using System;
using System.Linq.Expressions;
using CrudDatastore;

namespace CrudDatastore.Topper.Test.Specifications
{
    public class PersonSpecs : Specification<Entities.Person>
    {
        private PersonSpecs(Expression<Func<Entities.Person, bool>> predicate)
            : base(predicate)
        { }

        private PersonSpecs(string command, params object[] parameters)
            : base(command, parameters)
        { }

        public static PersonSpecs Get(int personId)
        {
            return new PersonSpecs(p => p.PersonId == personId);
        }

        public static PersonSpecs Get(string firstname, string lastname)
        {
            return new PersonSpecs(p => p.Firstname == firstname && p.Lastname == lastname);
        }

        public static PersonSpecs GetAll()
        {
            return new PersonSpecs(p => true);
        }
    }
}
