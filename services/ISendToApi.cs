using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileWatcherService.services
{
    public interface ISendToApi
    {
        Task PostAsJsonAsync(string JsonPath);
    }
}