using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpc.Server.Models;

namespace MyNoSqlGrpc.Server.Controllers
{
    [ApiController]
    [Route("Tables")]
    public class TablesController: Controller
    {
        private readonly DbTablesList _dbTablesList;

        public TablesController(DbTablesList dbTablesList)
        {
            _dbTablesList = dbTablesList;
        }
        
        [HttpGet("List")]
        public IEnumerable<TableModel> Index()
        {

            var tables = _dbTablesList.GetTables();
            return tables.Select(TableModel.Create);
        }

        
        [HttpPost("CreateIfNotExists")]
        public TableModel CreateIfNotExists([FromQuery]string tableName)
        {
            var table = _dbTablesList.CreateIfNotExists(tableName);
            return TableModel.Create(table);
        }
    }
}