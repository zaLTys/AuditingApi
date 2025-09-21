using Microsoft.AspNetCore.Mvc;
using AuditingApi.Models;

namespace AuditingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    private static readonly List<SampleData> _data = new();
    private static int _nextId = 1;

    [HttpGet]
    public ActionResult<IEnumerable<SampleData>> GetAll()
    {
        return Ok(_data);
    }

    [HttpGet("{id}")]
    public ActionResult<SampleData> GetById(int id)
    {
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null)
            return NotFound();
        
        return Ok(item);
    }

    [HttpPost]
    public ActionResult<SampleData> Create([FromBody] SampleData data)
    {
        if (data == null)
            return BadRequest("Data cannot be null");

        data.Id = _nextId++;
        data.CreatedAt = DateTime.UtcNow;
        _data.Add(data);
        
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
    }

    [HttpPut("{id}")]
    public ActionResult<SampleData> Update(int id, [FromBody] SampleData data)
    {
        if (data == null)
            return BadRequest("Data cannot be null");

        var existingItem = _data.FirstOrDefault(x => x.Id == id);
        if (existingItem == null)
            return NotFound();

        existingItem.Name = data.Name;
        existingItem.Description = data.Description;
        
        return Ok(existingItem);
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null)
            return NotFound();

        _data.Remove(item);
        return NoContent();
    }
}
