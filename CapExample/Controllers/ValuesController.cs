using System;
using System.Data;
using System.Threading.Tasks;
using Consul;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace CapExample.Controllers
{
    [Route("api/v1/[controller]")]
    public class ValuesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ICapPublisher _capBus;

        public ValuesController(
            IConfiguration configuration,
            ICapPublisher capPublisher)
        {
            this._configuration = configuration;
            _capBus = capPublisher;
        }

        [HttpGet]
        [Route("~/without/transaction")]
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.rabbitmq.mysql", DateTime.Now);

            return Ok();
        }

        [HttpGet]
        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new MySqlConnection(AppDbContext.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(_capBus, true))
                {
                    //your business code
                    connection.Execute("insert into test(name) values('test')", transaction: (IDbTransaction)transaction.DbTransaction);

                    //for (int i = 0; i < 5; i++)
                    //{
                    _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);
                    //}
                }
            }

            return Ok();
        }

        [HttpGet]
        [Route("~/ef/transaction")]
        public IActionResult EntityFrameworkWithTransaction([FromServices] AppDbContext dbContext)
        {
            using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: false))
            {
                dbContext.Persons.Add(new Person() { Name = "ef.transaction" });

                for (int i = 0; i < 1; i++)
                {
                    _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);
                }

                dbContext.SaveChanges();

                trans.Commit();
            }
            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.mysql")]
        public void Subscriber(DateTime p)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.mysql", Group = "group.test2")]
        public void Subscriber2(DateTime p, [FromCap] CapHeader header)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }


        [HttpGet]
        [Route("~/without/transaction/byConsul")]
        public async Task<IActionResult> WithoutTransactionByConsul()
        {
            string? consulAddress = _configuration["Consul:ConsulAddress"]?.ToString();
            if (string.IsNullOrWhiteSpace(consulAddress))
            {
                return BadRequest();
            }

            using (var consulClient = new ConsulClient(t => t.Address = new Uri(consulAddress)))
            {
                var services = consulClient.Catalog.Service(_configuration["Consul:NodeName"]).Result.Response;

                if (services != null && services.Any())
                {
                    //模拟随机一台进行请求，这里只是测试，可以选择合适的负载均衡框架
                    Random r = new Random();
                    int index = r.Next(services.Count());
                    var service = services.ElementAt(index);

                    using (HttpClient client = new HttpClient())
                    {
                        //请求服务WebApi1
                        var response = await client.GetAsync($"http://{service.ServiceAddress}:{service.ServicePort}/without/transaction");
                        string result = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            return Ok();
        }
    }
}
