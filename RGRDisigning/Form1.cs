using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RGRDisigning
{
    public partial class Form1 : Form
    {
        private const int MaxPagesToVisit = 100; // Максимальное количество страниц для посещения
        private int pagesVisited;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            txtResults.Text = null;
            txtResults.Text = "------------------------------------------Подождите идет поиск------------------------------------------";
            string url = "https://ru.wikipedia.org/wiki/Сисигамбис"; // Замените на нужный URL
            string words = txtWords.Text;
            List<string> wordList = words.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
          
            pagesVisited = 0; // Сброс счетчика посещенных страниц
            List<string> foundWordsAndLinks = CrawlWebPage(url, wordList, 2); // Начальная страница и глубина обхода

            txtResults.Text = string.Join(Environment.NewLine, foundWordsAndLinks);
        }

        private List<string> CrawlWebPage(string url, List<string> words, int depth)
        {
            HashSet<string> visited = new HashSet<string>();
            List<(string Url, Dictionary<string, int> WordCounts)> results = new List<(string, Dictionary<string, int>)>();

            void Crawl(string currentUrl, int currentDepth)
            {
                if (currentDepth > depth || visited.Contains(currentUrl) || pagesVisited >= MaxPagesToVisit) return;
                visited.Add(currentUrl);
                pagesVisited++;

                var wordsOnPage = ParseWebPage(currentUrl, words);
                if (wordsOnPage.Count > 0)
                {
                    results.Add((currentUrl, wordsOnPage));
                }

                HtmlWeb web = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(currentUrl);

                foreach (string link in GetLinksFromPage(doc.DocumentNode))
                {
                    if (pagesVisited >= MaxPagesToVisit) break; // Прерывание при достижении лимита
                    Crawl(link, currentDepth + 1);
                }
            }

            Crawl(url, 0);

            // Сортировка результатов по количеству встреченных слов
            var sortedResults = results
                .OrderByDescending(result => result.WordCounts.Values.Sum())
                .Select(result => $"{"<<-- "+result.Url+" -->>"} - {string.Join(", ", result.WordCounts.Select(kv => $"Найдено совпадений: {kv.Key}: {kv.Value} "))}\n")
                .ToList();
            Cursor = Cursors.Default;
            txtResults.Text = null;
            return sortedResults;
        }

        private Dictionary<string, int> ParseWebPage(string url, List<string> words)
        {
            Dictionary<string, int> wordCounts = new Dictionary<string, int>();
            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url);

            string text = GetTextFromHtml(doc.DocumentNode);
            List<string> textWords = text.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var word in words)
            {
                int count = textWords.Count(tw => tw.Equals(word, StringComparison.OrdinalIgnoreCase));
                if (count > 0)
                {
                    wordCounts[word] = count;
                }
            }

            return wordCounts;
        }

        private string GetTextFromHtml(HtmlNode node)
        {
            if (node == null)
                return string.Empty;

            if (node.NodeType == HtmlNodeType.Text)
                return node.InnerText;

            return string.Join(" ", node.ChildNodes.Select(child => GetTextFromHtml(child)));
        }

        private List<string> GetLinksFromPage(HtmlNode node)
        {
            List<string> links = new List<string>();

            foreach (HtmlNode link in node.SelectNodes("//a[@href]"))
            {
                HtmlAttribute attr = link.Attributes["href"];
                if (attr != null)
                {
                    string hrefValue = attr.Value;
                    if (Uri.IsWellFormedUriString(hrefValue, UriKind.RelativeOrAbsolute))
                    {
                        Uri baseUri = new Uri("https://ru.wikipedia.org");
                        Uri absoluteUri = new Uri(baseUri, hrefValue);
                        links.Add(absoluteUri.ToString());
                    }
                }
            }

            return links;
        }
    }
}
