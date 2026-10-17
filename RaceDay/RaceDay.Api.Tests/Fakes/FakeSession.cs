using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Tests.Fakes;

//stands in for the real session, keeps everything in a dictionary instead of the servers memory cache
//SetInt32, GetString etc still work on it bcs theyre built on top of Set and TryGetValue
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = new();

    public bool IsAvailable => true;

    public string Id { get; } = Guid.NewGuid().ToString();

    public IEnumerable<string> Keys => _values.Keys;

    public void Clear() => _values.Clear();

    //nothing to load or save, the values are already sitting in the dictionary
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _values.Remove(key);

    public void Set(string key, byte[] value) => _values[key] = value;

    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _values.TryGetValue(key, out value);
}