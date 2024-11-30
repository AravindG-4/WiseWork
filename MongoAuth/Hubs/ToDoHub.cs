using Microsoft.AspNetCore.SignalR;
using MongoAuth.Shared.Models;
using MongoAuth.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;

namespace MongoAuth.Hubs
{
    //[Authorize]
    public class ToDoHub : Hub
    {
        private readonly MongoDBServices _mongoToDoService;
        //private IConfiguration configuration;

        public ToDoHub(MongoDBServices mongoToDoService)
        {
            _mongoToDoService = mongoToDoService;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated ?? false)
            {
                await base.OnConnectedAsync();
            }
            else
            {
                Context.Abort();
            }
        }

        public async Task<List<ToDo>> ListPending()
        {
            return await _mongoToDoService.ReadPending();
        }

        public async Task<List<ToDo>> ListCompleted()
        {
            return await _mongoToDoService.ReadCompleted();
        }

        public async Task CreateToDo(string Title, DateTime? TaskAddedDate)
        {
            ToDo task = new ToDo()
            {
                Title = Title,
                TaskAddedDate = TaskAddedDate
            };
            await _mongoToDoService.CreateToDo(task);
            await Clients.All.SendAsync("Created", Title);
        }

        public async Task CompleteToDo(ToDo task)
        {
            await _mongoToDoService.CompleteToDo(task);
            await Clients.All.SendAsync("Completed", task.Title);
        }

        public async Task RemoveToDo(ToDo task)
        {
            await _mongoToDoService.RemoveToDo(task);
            await Clients.All.SendAsync("Removed", task.Title);
        }
    }
}
