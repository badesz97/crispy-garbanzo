using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiBase.Data
{
    public interface IEntity<T> where T : class
    {
        string Id { get; set; }

        AppUser Owner { get; set; }

        void CopyFrom(T another);
    }
}
