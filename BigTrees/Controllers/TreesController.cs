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

        //// GET: api/Trees/?kommun=kommun
        //[ResponseType(typeof(treesTrunc))]
        //public string GetTreesByKommun(string kommun)
        //{
        //    var result = new List<treesTrunc>();
        //    var resultQuery = (from tree in db.treesTruncs
        //                       where tree.kommun == kommun
        //                       orderby tree.stamomkret descending
        //                       select tree).Take(500);

        //    result = resultQuery.ToList();
        //    if (result == null)
        //    {
        //        return "no results";
        //    }
        //    return JsonConvert.SerializeObject(result);
        //}

        //// GET: api/Trees/?stamomkret=stamomkret
        //[ResponseType(typeof(treesTrunc))]
        //public string GetTreesByCircumference(int stamomkret)
        //{
        //    var result = new List<treesTrunc>();
        //    var resultQuery = (from tree in db.treesTruncs
        //                       where tree.stamomkret > stamomkret
        //                       orderby tree.stamomkret descending
        //                       select tree).Take(500);

        //    result = resultQuery.ToList();
        //    if (result == null)
        //    {
        //        return JsonConvert.SerializeObject(NotFound());
        //    }
        //    return JsonConvert.SerializeObject(result);
        //    //return Ok(result);
        //}

        //// GET: api/Trees/?tradslag=tradslag
        //[ResponseType(typeof(treesTrunc))]
        //public string GetTreesByTreeType(string tradslag)
        //{
        //    var result = new List<treesTrunc>();
        //    var resultQuery = (from tree in db.treesTruncs
        //                       where tree.tradslag.Contains(tradslag)
        //                       orderby tree.stamomkret descending
        //                       select tree).Take(500);

        //    result = resultQuery.ToList();
        //    if (result == null)
        //    {
        //        return JsonConvert.SerializeObject(NotFound());
        //    }
        //    return JsonConvert.SerializeObject(result);
        //    //return Ok(result);
        //}

        //// GET: api/Trees/?kommun=kommun
        //[ResponseType(typeof(treesTrunc))]
        //public string GetTreesByKommun(Expression<Func<treesTrunc, bool>> whereClause)
        //{
        //    var result = new List<treesTrunc>();

        //    var resultQuery = db.treesTruncs.Where(whereClause)
        //                        .OrderBy(q => q.stamomkret)
        //                        .Take(1000);
        //    //var resultQuery = (from tree in db.treesTruncs
        //    //                   where whereClause 
        //    //                   orderby tree.stamomkret descending
        //    //                   select tree).Take(500);

        //    result = resultQuery.ToList();
        //    if (result == null)
        //    {
        //        return JsonConvert.SerializeObject(NotFound());
        //    }
        //    return JsonConvert.SerializeObject(result);
        //    //return Ok(result);
        //}


        //// GET: api/Trees/5
        //[ResponseType(typeof(treesTrunc))]
        //public async Task<IHttpActionResult> GetTrees(int id)
        //{
        //  treesTrunc treesTrunc = await db.treesTruncs.FindAsync(id);
        //  if (treesTrunc == null)
        //  {
        //    return NotFound();
        //  }

        //  return Ok(treesTrunc);
        //}

        //[ResponseType(typeof(treesTrunc))]
        //public async Task<IHttpActionResult> GetTrees(string queryString)
        //{
        //  var queryString = actionContext.Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
        //  treesTrunc treesTrunc = await db.treesTruncs.FindAsync(id);
        //  if (treesTrunc == null)
        //  {
        //    return NotFound();
        //  }

        //  return Ok(treesTrunc);
        //}



        //internal class GeoJsonListConverter : JsonConverter
        //{
        //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        //    {
        //        throw new System.NotImplementedException();
        //    }

        //    public override bool CanConvert(Type objectType)
        //    {
        //        return typeof(PagedList<User>).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        //    }

        //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        //    {
        //        if (reader.TokenType == JsonToken.StartObject)
        //        {
        //            JObject item = JObject.Load(reader);

        //            if (item["users"] != null)
        //            {
        //                var users = item["users"].ToObject<IList<User>>(serializer);

        //                int length = item["length"].Value<int>();
        //                int limit = item["limit"].Value<int>();
        //                int start = item["start"].Value<int>();
        //                int total = item["total"].Value<int>();

        //                return new PagedList<User>(users, new PagingInformation(start, limit, length, total));
        //            }
        //        }
        //        else
        //        {
        //            JArray array = JArray.Load(reader);

        //            var users = array.ToObject<IList<User>>();

        //            return new PagedList<User>(users);
        //        }

        //        // This should not happen. Perhaps better to throw exception at this point?
        //        return null;
        //    }
        //}


        // PUT: api/Trees/5
        //[ResponseType(typeof(void))]
        //public async Task<IHttpActionResult> PuttreesTrunc(int id, treesTrunc treesTrunc)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != treesTrunc.ogr_fid)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(treesTrunc).State = EntityState.Modified;

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!treesTruncExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        //// POST: api/Trees
        //[ResponseType(typeof(treesTrunc))]
        //public async Task<IHttpActionResult> PosttreesTrunc(treesTrunc treesTrunc)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.treesTruncs.Add(treesTrunc);
        //    await db.SaveChangesAsync();

        //    return CreatedAtRoute("DefaultApi", new { id = treesTrunc.ogr_fid }, treesTrunc);
        //}

        //// DELETE: api/Trees/5
        //[ResponseType(typeof(treesTrunc))]
        //public async Task<IHttpActionResult> DeletetreesTrunc(int id)
        //{
        //    treesTrunc treesTrunc = await db.treesTruncs.FindAsync(id);
        //    if (treesTrunc == null)
        //    {
        //        return NotFound();
        //    }

        //    db.treesTruncs.Remove(treesTrunc);
        //    await db.SaveChangesAsync();

        //    return Ok(treesTrunc);
        //}

    }
}