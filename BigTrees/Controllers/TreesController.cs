using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Newtonsoft.Json;
using System.Web.Http.Cors;
using System.Linq.Dynamic;
using BigTrees.Models;

using System;
using System.Net.Http;
using BigTrees;
using System.Web.Http.Results;
using System.Web.Mvc;
using System.Linq.Expressions;
using System.Threading;

namespace BigTrees.Controllers
{
    [EnableCors(origins: "http://localhost:8080", headers: "*", methods: "*")]
    public class TreesController : ApiController
    {
        private BigTreesContext db = new BigTreesContext();

        // GET: api/Trees
        //public IQueryable<treesTrunc> GettreesTruncs()
        //{
        //    return db.treesTruncs;
        //}

        // GET: api/Trees/?whereClause=whereClause
        [ResponseType(typeof(treesTrunc))]
        public string GetTreesByWhereClause(string whereClause)
        {
            if (whereClause == null)
            {
                whereClause = "1=1";
            }
            var result = new List<treesTrunc>();
            var resultQuery = db.treesTruncs
                                    .Where(whereClause)
                                    //.OrderBy("stamomkret")
                                    .OrderByDescending(t => t.stamomkret)
                                    .Take(500);

            result = resultQuery.ToList();

            var treeDropdown = new List<string>();
            var treeQuery = db.treesTruncs
                               .Where(whereClause)
                               .Select(t=> t.tradslag)
                               .Distinct();
            treeDropdown = treeQuery.ToList();

            var kommunDropdown = new List<string>();
            var kommunQuery = db.treesTruncs
                               .Where(whereClause)
                               .Select(t => t.kommun)
                               .Distinct();
            kommunDropdown = kommunQuery.ToList();


            decimal maxStamomkret = db.treesTruncs
                               .Where(whereClause)
                               .Max(t => t.stamomkret)
                               .Value;

            if (result == null)
            {
                return "no results";
            }
            return JsonConvert.SerializeObject(new { result, treeDropdown, kommunDropdown, maxStamomkret } );
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("Trees/SearchVisibleMap/")]
        [ResponseType(typeof(treesTrunc))]
        public string GetTreesByBoundingBox(decimal minLong, decimal minLat, decimal maxLong, decimal maxLat)
        {
            var result = new List<treesTrunc>();
            var resultQuery = (from tree in db.treesTruncs
                               where tree.latitude >= minLat && tree.latitude <= maxLat
                                  && tree.longitude >= minLong && tree.longitude <= maxLong
                               orderby tree.stamomkret descending
                               select tree).Take(500);

            result = resultQuery.ToList();
            if (result == null)
            {
                return "no results";
            }
            return JsonConvert.SerializeObject(result);
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("Trees/GetTreeCounts/")]
        [ResponseType(typeof(treeTotal))]
        public IHttpActionResult GetTreeCounts(string kommun)
        {
            if (kommun == null)
            {
                kommun = "1=1";
            }
            var result = new List<treeTotal>();
            var resultQuery = db.treesTruncs
                                    .Where(kommun)
                                    .GroupBy(t => t.tradslag)
                                    .Select(group => new treeTotal()
                                    {
                                        tradslag = group.Key,
                                        treeCount = group.Count()
                                    })
                                    .OrderByDescending(t => t.treeCount);

            result = resultQuery.ToList();
            //if (result == null)
            //{
            //    return "no results";
            //}
            return new TextResult(JsonConvert.SerializeObject(result), Request);
        }


        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("Trees/AverageCircumference/")]
        [ResponseType(typeof(avgCircumferenceModel))]
        public IHttpActionResult AverageCircumference(string whereClause)
        {
            if (whereClause == null)
            {
                whereClause = "1=1";
            }
            var result = new List<avgCircumferenceModel>();
            var resultQuery = db.treesTruncs
                                    .Where(whereClause)
                                    .GroupBy(g => g.tradslag, g => g.stamomkret)
                                    //.Average(t => t.stamomkret);
                                    .Select(g => new avgCircumferenceModel()
                                    {
                                        tradslag = g.Key,
                                        avgStamomkret = g.Average().HasValue ? Decimal.Round(g.Average().Value) : 0,
                                        count = g.Count()
                                    })
                                    .OrderByDescending(t => t.avgStamomkret);

            result = resultQuery.ToList();
            //if (result == null)
            //{
            //    return "no results";
            //}
            return new TextResult(JsonConvert.SerializeObject(result), Request);
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("Trees/Largest20/")]
        [ResponseType(typeof(treesTrunc))]
        public IHttpActionResult Largest20(string whereClause)
        {
            if (whereClause == null)
            {
                whereClause = "1=1";
            }
            var result = new List<treesTrunc>();
            var resultQuery = db.treesTruncs
                                    .Where(whereClause)
                                    .OrderByDescending(t => t.stamomkret)
                                    .Take(20);

            result = resultQuery.ToList();
            //if (result == null)
            //{
            //    return "no results";
            //}
            return new TextResult(JsonConvert.SerializeObject(result), Request);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public class TextResult : IHttpActionResult
        {
            string _value;
            HttpRequestMessage _request;

            public TextResult(string value, HttpRequestMessage request)
            {
                _value = value;
                _request = request;
            }
            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage()
                {
                    Content = new StringContent(_value),
                    RequestMessage = _request
                };
                return Task.FromResult(response);
            }
        }

        private bool treesTruncExists(int id)
        {
            return db.treesTruncs.Count(e => e.ogr_fid == id) > 0;
        }
    }
}