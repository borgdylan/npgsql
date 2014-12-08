#if ENTITIES7
// NpgsqlMigrationSqlGenerator.cs
//
// Author:
//    David Karlaš (david.karlas@gmail.com)
//
//    Copyright (C) 2014 David Karlaš
//
// Permission to use, copy, modify, and distribute this software and its
// documentation for any purpose, without fee, and without a written
// agreement is hereby granted, provided that the above copyright notice
// and this paragraph and the following two paragraphs appear in all copies.
//
// IN NO EVENT SHALL THE NPGSQL DEVELOPMENT TEAM BE LIABLE TO ANY PARTY
// FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES,
// INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS
// DOCUMENTATION, EVEN IF THE NPGSQL DEVELOPMENT TEAM HAS BEEN ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//
// THE NPGSQL DEVELOPMENT TEAM SPECIFICALLY DISCLAIMS ANY WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE. THE SOFTWARE PROVIDED HEREUNDER IS
// ON AN "AS IS" BASIS, AND THE NPGSQL DEVELOPMENT TEAM HAS NO OBLIGATIONS
// TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Migrations.Model;

namespace Npgsql
{
    public class NpgsqlMigrationSqlGenerator : MigrationCodeGenerator
    {
		
		public NpgsqlMigrationSqlGenerator(ModelCodeGenerator modelCodeGenerator) : base(modelCodeGenerator) {
		}
		
		public override void Generate ( AddPrimaryKeyOperation addPrimaryKeyOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropDefaultConstraintOperation dropDefaultConstraintOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( AddDefaultConstraintOperation addDefaultConstraintOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( AlterColumnOperation alterColumnOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( RenameColumnOperation renameColumnOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropColumnOperation dropColumnOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( AddColumnOperation addColumnOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropPrimaryKeyOperation dropPrimaryKeyOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( CopyDataOperation copyDataOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( RenameIndexOperation renameIndexOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropIndexOperation dropIndexOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( CreateIndexOperation createIndexOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropForeignKeyOperation dropForeignKeyOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( AddForeignKeyOperation addForeignKeyOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropUniqueConstraintOperation dropUniqueConstraintOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( AddUniqueConstraintOperation addUniqueConstraintOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( MoveTableOperation dropTableOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( CreateDatabaseOperation createDatabaseOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropDatabaseOperation dropDatabaseOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( CreateSequenceOperation createSequenceOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropSequenceOperation dropSequenceOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( RenameSequenceOperation renameSequenceOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( SqlOperation sqlOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( RenameTableOperation dropTableOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( DropTableOperation dropTableOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( CreateTableOperation createTableOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( AlterSequenceOperation alterSequenceOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void Generate ( MoveSequenceOperation moveSequenceOperation,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}
		
		public override void GenerateMigrationClass(string _namespace,  string className,  MigrationInfo migration,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

		public override void GenerateMigrationMetadataClass ( string _namespace,  string className,  MigrationInfo migration,  Type contextType,  IndentedStringBuilder stringBuilder) {
			throw new NotImplementedException();
		}

	}
}

#endif