﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlRepoEx.SqlServer
{
    public class SelectStatementSpecification
    {
        private const string StatementTemplate = "{0}{1}{2}{3}{4}{5}";

        public SelectStatementSpecification()
        {
            this.Columns = new List<ColumnSpecification>();
            this.Tables = new List<SelectStatementTableSpecification>();
            this.Joins = new List<JoinSpecification>();
            this.Filters = new List<FilterGroup>();
            this.Orderings = new List<OrderSpecification>();
            this.Groupings = new List<GroupSpecification>();
            this.Havings = new List<SelectStatementHavingSpecification>();
        }

        public IList<ColumnSpecification> Columns { get; }
        public IList<GroupSpecification> Groupings { get; }
        public IList<SelectStatementHavingSpecification> Havings { get; }
        public IList<JoinSpecification> Joins { get; }
        public IList<OrderSpecification> Orderings { get; }
        public IList<SelectStatementTableSpecification> Tables { get; }
        public IList<FilterGroup> Filters { get; private set; }
        public bool NoLocks { private get; set; }
        public int? Top { get; internal set; }

        public int? Page { get; internal set; }
        public bool UseTopPercent { get; internal set; }

        public override string ToString()
        {
            var selectClause = this.BuildSelectClause();
            var fromClause = this.BuildFromClause();
            var whereClause = this.BuildWhereClause();
            var orderByClause = this.BuildOrderByClause();
            var groupByClause = this.BuildGroupByClause();
            var havingClause = this.BuildHavingClause();
            string result = string.Format(StatementTemplate,
                selectClause,
                fromClause,
                whereClause,
                groupByClause,
                orderByClause,
                havingClause);

            return BuildPageClause(result);
        }

        private string BuildFromClause()
        {
            var result = string.Empty;
            foreach (var specification in this.Tables)
            {
                specification.NoLocks = this.NoLocks;
                result += specification.ToString();
                if (specification.JoinType == JoinType.None)
                {
                    continue;
                }

                result = this.Joins.Where(joinSpecification =>
                                              joinSpecification.RightEntityType == specification.EntityType
                                              && joinSpecification.RightTableAlias == specification.Alias)
                             .Aggregate(result,
                                 (current, joinSpecification) => current + joinSpecification.ToString());
            }

            return result;
        }

        private string BuildGroupByClause()
        {
            return !this.Groupings.Any() ? string.Empty : $"\nGROUP BY {string.Join("\n, ", this.Groupings)}";
        }

        private string BuildHavingClause()
        {
            return !this.Havings.Any() ? string.Empty : $"\nHAVING {string.Join("\n, ", this.Havings)}";
        }

        private string BuildOrderByClause()
        {
            if (Page.HasValue)
            {
                return string.Empty;
            }
            return !this.Orderings.Any() ? string.Empty : $"\nORDER BY {string.Join("\n, ", this.Orderings)}";
        }

        private string BuildPageOrderByClause()
        {
            return !this.Orderings.Any() ? string.Empty : $"\nORDER BY {string.Join("\n, ", this.Orderings)}";
        }

        private string BuildPageClause(string sql)
        {
            if (!Page.HasValue)
            {
                return sql + ";";
            }
            string orderbystr = BuildPageOrderByClause();
            if (string.IsNullOrWhiteSpace(orderbystr))
            {
                return sql + ";";
            }
            const string PageTemplage = "{0} FROM ({1})As __Page_Query WHERE row_number > {2};";
            string selectpage = BuildSelectPageClause();
            string currertpage = ((Page ?? 1 - 1) * Top).ToString();


            return string.Format(PageTemplage, selectpage, sql, currertpage);
        }

        private string BuildSelectPageClause()
        {
            const string ClauseTemplate = "SELECT {0}{1}";

            string top = Top.HasValue ? $"TOP ({Top}) " : "TOP (20) ";

            var selections = string.Join("\n, ",
                this.Columns.Select(c => c.ToString())
                    .ToArray());

            return string.Format(ClauseTemplate, top, "*");
        }

        private string BuildSelectClause()
        {
            const string ClauseTemplate = "SELECT {0}{1}{2}";


            string top = string.Empty;
            string page = string.Empty;
            if (Page.HasValue)
            {
                string orderbystr = BuildPageOrderByClause();
                if (!string.IsNullOrWhiteSpace(orderbystr))
                {
                    page = $"row_number() OVER ( {orderbystr}) as row_number,";
                }
                else
                {
                    top = Top.HasValue ? $"TOP ({Top}) " : string.Empty;
                }
            }
            else
            {
                top = Top.HasValue ? $"TOP ({Top}) " : string.Empty;
            }

            var selections = string.Join("\n, ",
                this.Columns.Select(c => c.ToString())
                    .ToArray());

            return string.Format(ClauseTemplate, top, page, string.IsNullOrEmpty(selections) ? "*" : selections);
        }

        private string BuildWhereClause()
        {
            return !this.Filters.Any() ? string.Empty : $"\n{string.Join("\n", this.Filters)}";
        }
    }
}