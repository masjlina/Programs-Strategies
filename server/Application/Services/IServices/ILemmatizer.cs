using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services.IServices;

public interface ILemmatizer
{
    string Lemmatize(string word);
    Task<List<string>> LemmatizeBatchAsync(List<string> words);
}
