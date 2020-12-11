using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MyNoSqlGrpc.Server.Models;

namespace MyNoSqlGrpc.Server.Controllers
{
    [ApiController]
    [Route("Tables")]
    public class TablesController: Controller
    {
        
        [HttpGet("List")]
        public IEnumerable<TableModel> Index()
        {

            var tables = ServiceLocator.DbTablesList.GetTables();
            return tables.Select(TableModel.Create);
        }

        
        [HttpPost("CreateIfNotExists")]
        public TableModel CreateIfNotExists([FromQuery]string tableName)
        {
            var table = ServiceLocator.DbTablesList.CreateIfNotExists(tableName);
            return TableModel.Create(table);
        }
    }
}