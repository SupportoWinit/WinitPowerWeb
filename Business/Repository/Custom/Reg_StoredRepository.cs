using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Data;
using Domain;
using System.Text;
using Common;
using System.IO;
using System.Globalization;
using Business.MDBSchema;
using System.Collections;
using System.Data.Entity.Core.Objects;
using System.Linq.Expressions;
using log4net;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Reflection;

namespace Business.Repository.Custom
{
    public class Reg_StoredRepository : GenericRepository<Reg_Stored>, IReg_StoredRepository
    {
        public Reg_StoredRepository(PowerWebEntities context)
            : base(context)
        {
        }
    }
}
