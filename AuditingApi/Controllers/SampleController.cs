using Microsoft.AspNetCore.Mvc;
using AuditingApi.Models;
using System.Collections.Concurrent;

namespace AuditingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    private static readonly ConcurrentDictionary<int, SampleData> _data = new();
    private static int _nextId = 1;

    [HttpGet]
    public ActionResult<IEnumerable<SampleData>> GetAll()
    {
        return Ok(_data.Values);
    }

    [HttpGet("{id}")]
    public ActionResult<SampleData> GetById(int id)
    {
        if (_data.TryGetValue(id, out var item))
        {
            return Ok(item);
        }
        
        return NotFound();
    }

    [HttpPost]
    public ActionResult<SampleData> Create([FromBody] SampleData data)
    {
        if (data == null)
            return BadRequest("Data cannot be null");

        data.Id = Interlocked.Increment(ref _nextId);
        data.CreatedAt = DateTime.UtcNow;
        _data.TryAdd(data.Id, data);
        
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
    }

    [HttpPut("{id}")]
    public ActionResult<SampleData> Update(int id, [FromBody] SampleData data)
    {
        if (data == null)
            return BadRequest("Data cannot be null");

        if (_data.TryGetValue(id, out var existingItem))
        {
            existingItem.Name = data.Name;
            existingItem.Description = data.Description;
            return Ok(existingItem);
        }

        return NotFound();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        if (_data.TryRemove(id, out _))
        {
            return NoContent();
        }

        return NotFound();
    }
}
