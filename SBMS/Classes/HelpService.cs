using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SBMS.Classes
{
    public class HelpService
    {
        private readonly string _connStr;

        public HelpService()
        {
            _connStr = Config.GetRawConnectionString();
        }

        // ── Lookup ──────────────────────────────────────────────────────────

        public HelpArticle GetById(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "SELECT Id, Question, Answer, Keywords, PageUrl, Module, UsageCount, CreatedBy, CreatedDate, Published, CompanyID " +
                "FROM HelpArticles WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read()) return Map(rdr);
                }
            }
            return null;
        }

        /// <summary>Find published articles matching the search terms, biased by module, ordered by popularity.</summary>
        public List<HelpArticle> Search(string query, string module, long? companyId, int maxResults = 5)
        {
            var words = (query ?? "").ToLower()
                .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            List<HelpArticle> results;

            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "SELECT Id, Question, Answer, Keywords, PageUrl, Module, UsageCount, CreatedBy, CreatedDate, Published, CompanyID " +
                "FROM HelpArticles WHERE Published = 1", conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    var all = new List<HelpArticle>();
                    while (rdr.Read()) all.Add(Map(rdr));

                    results = all
                        .Where(a => words.Count == 0 || words.Any(w =>
                            (a.Keywords ?? "").ToLower().Contains(w) ||
                            (a.Question ?? "").ToLower().Contains(w)))
                        .OrderByDescending(a =>
                        {
                            int score = words.Count(w =>
                                (a.Keywords ?? "").ToLower().Contains(w) ||
                                (a.Question ?? "").ToLower().Contains(w));
                            if (!string.IsNullOrEmpty(module) &&
                                string.Equals(a.Module, module, StringComparison.OrdinalIgnoreCase))
                                score += 2;
                            return score;
                        })
                        .ThenByDescending(a => a.UsageCount)
                        .Take(maxResults)
                        .ToList();
                }
            }

            return results;
        }

        // ── CRUD ────────────────────────────────────────────────────────────

        public int Insert(HelpArticle article)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "INSERT INTO HelpArticles (Question, Answer, Keywords, PageUrl, Module, UsageCount, CreatedBy, CreatedDate, Published, CompanyID) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@Question, @Answer, @Keywords, @PageUrl, @Module, @UsageCount, @CreatedBy, @CreatedDate, @Published, @CompanyID)", conn))
            {
                AddParams(cmd, article);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public void Update(HelpArticle article)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "UPDATE HelpArticles SET Question=@Question, Answer=@Answer, Keywords=@Keywords, PageUrl=@PageUrl, " +
                "Module=@Module, UsageCount=@UsageCount, Published=@Published " +
                "WHERE Id=@Id", conn))
            {
                AddParams(cmd, article);
                cmd.Parameters.AddWithValue("@Id", article.Id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void IncrementUsage(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "UPDATE HelpArticles SET UsageCount = UsageCount + 1 WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand("DELETE FROM HelpArticles WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ── Admin ───────────────────────────────────────────────────────────

        public List<HelpArticle> GetUnpublished()
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "SELECT Id, Question, Answer, Keywords, PageUrl, Module, UsageCount, CreatedBy, CreatedDate, Published, CompanyID " +
                "FROM HelpArticles WHERE Published = 0 ORDER BY CreatedDate DESC", conn))
            {
                conn.Open();
                var list = new List<HelpArticle>();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(Map(rdr));
                return list;
            }
        }

        public List<HelpArticle> GetAllPublished()
        {
            using (var conn = new SqlConnection(_connStr))
            using (var cmd = new SqlCommand(
                "SELECT Id, Question, Answer, Keywords, PageUrl, Module, UsageCount, CreatedBy, CreatedDate, Published, CompanyID " +
                "FROM HelpArticles WHERE Published = 1 ORDER BY UsageCount DESC", conn))
            {
                conn.Open();
                var list = new List<HelpArticle>();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) list.Add(Map(rdr));
                return list;
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private HelpArticle Map(SqlDataReader rdr)
        {
            return new HelpArticle
            {
                Id          = (int)rdr["Id"],
                Question    = rdr["Question"] as string,
                Answer      = rdr["Answer"] as string,
                Keywords    = rdr["Keywords"] as string,
                PageUrl     = rdr["PageUrl"] as string,
                Module      = rdr["Module"] as string,
                UsageCount  = (int)rdr["UsageCount"],
                CreatedBy   = rdr["CreatedBy"] as string,
                CreatedDate = (DateTime)rdr["CreatedDate"],
                Published   = (bool)rdr["Published"],
                CompanyID   = rdr["CompanyID"] as long?
            };
        }

        private void AddParams(SqlCommand cmd, HelpArticle a)
        {
            cmd.Parameters.AddWithValue("@Question",    (object)a.Question    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Answer",      (object)a.Answer      ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Keywords",    (object)a.Keywords    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PageUrl",     (object)a.PageUrl     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Module",      (object)a.Module      ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UsageCount",  a.UsageCount);
            cmd.Parameters.AddWithValue("@CreatedBy",   (object)a.CreatedBy   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedDate", a.CreatedDate);
            cmd.Parameters.AddWithValue("@Published",   a.Published);
            cmd.Parameters.AddWithValue("@CompanyID",   (object)a.CompanyID   ?? DBNull.Value);
        }
    }
}
