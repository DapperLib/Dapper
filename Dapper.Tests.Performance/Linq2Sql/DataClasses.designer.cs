﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Dapper.Tests.Performance.Linq2Sql
{
    using System.Data.Linq;
    using System.Data.Linq.Mapping;
    using System.Data;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using System.ComponentModel;
    using System;


    [global::System.Data.Linq.Mapping.DatabaseAttribute(Name = "tempdb")]
    public partial class DataClassesDataContext : System.Data.Linq.DataContext
    {

        private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();

        #region Extensibility Method Definitions
        partial void OnCreated();
        partial void InsertPost(Post instance);
        partial void UpdatePost(Post instance);
        partial void DeletePost(Post instance);
        #endregion

        public DataClassesDataContext(string connection) :
                base(connection, mappingSource)
        {
            OnCreated();
        }

        public DataClassesDataContext(System.Data.IDbConnection connection) :
                base(connection, mappingSource)
        {
            OnCreated();
        }

        public DataClassesDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) :
                base(connection, mappingSource)
        {
            OnCreated();
        }

        public DataClassesDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) :
                base(connection, mappingSource)
        {
            OnCreated();
        }

        public System.Data.Linq.Table<Post> Posts
        {
            get
            {
                return this.GetTable<Post>();
            }
        }
    }

    [global::System.Data.Linq.Mapping.TableAttribute(Name = "dbo.Posts")]
    public partial class Post : INotifyPropertyChanging, INotifyPropertyChanged
    {

        private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);

        private int _Id;

        private string _Text;

        private System.DateTime _CreationDate;

        private System.DateTime _LastChangeDate;

        private System.Nullable<int> _Counter1;

        private System.Nullable<int> _Counter2;

        private System.Nullable<int> _Counter3;

        private System.Nullable<int> _Counter4;

        private System.Nullable<int> _Counter5;

        private System.Nullable<int> _Counter6;

        private System.Nullable<int> _Counter7;

        private System.Nullable<int> _Counter8;

        private System.Nullable<int> _Counter9;

        #region Extensibility Method Definitions
        partial void OnLoaded();
        partial void OnValidate(System.Data.Linq.ChangeAction action);
        partial void OnCreated();
        partial void OnIdChanging(int value);
        partial void OnIdChanged();
        partial void OnTextChanging(string value);
        partial void OnTextChanged();
        partial void OnCreationDateChanging(System.DateTime value);
        partial void OnCreationDateChanged();
        partial void OnLastChangeDateChanging(System.DateTime value);
        partial void OnLastChangeDateChanged();
        partial void OnCounter1Changing(System.Nullable<int> value);
        partial void OnCounter1Changed();
        partial void OnCounter2Changing(System.Nullable<int> value);
        partial void OnCounter2Changed();
        partial void OnCounter3Changing(System.Nullable<int> value);
        partial void OnCounter3Changed();
        partial void OnCounter4Changing(System.Nullable<int> value);
        partial void OnCounter4Changed();
        partial void OnCounter5Changing(System.Nullable<int> value);
        partial void OnCounter5Changed();
        partial void OnCounter6Changing(System.Nullable<int> value);
        partial void OnCounter6Changed();
        partial void OnCounter7Changing(System.Nullable<int> value);
        partial void OnCounter7Changed();
        partial void OnCounter8Changing(System.Nullable<int> value);
        partial void OnCounter8Changed();
        partial void OnCounter9Changing(System.Nullable<int> value);
        partial void OnCounter9Changed();
        #endregion

        public Post()
        {
            OnCreated();
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Id", AutoSync = AutoSync.OnInsert, DbType = "Int NOT NULL IDENTITY", IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id
        {
            get
            {
                return this._Id;
            }
            set
            {
                if ((this._Id != value))
                {
                    this.OnIdChanging(value);
                    this.SendPropertyChanging();
                    this._Id = value;
                    this.SendPropertyChanged("Id");
                    this.OnIdChanged();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Text", DbType = "VarChar(MAX) NOT NULL", CanBeNull = false)]
        public string Text
        {
            get
            {
                return this._Text;
            }
            set
            {
                if ((this._Text != value))
                {
                    this.OnTextChanging(value);
                    this.SendPropertyChanging();
                    this._Text = value;
                    this.SendPropertyChanged("Text");
                    this.OnTextChanged();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_CreationDate", DbType = "DateTime NOT NULL")]
        public System.DateTime CreationDate
        {
            get
            {
                return this._CreationDate;
            }
            set
            {
                if ((this._CreationDate != value))
                {
                    this.OnCreationDateChanging(value);
                    this.SendPropertyChanging();
                    this._CreationDate = value;
                    this.SendPropertyChanged("CreationDate");
                    this.OnCreationDateChanged();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_LastChangeDate", DbType = "DateTime NOT NULL")]
        public System.DateTime LastChangeDate
        {
            get
            {
                return this._LastChangeDate;
            }
            set
            {
                if ((this._LastChangeDate != value))
                {
                    this.OnLastChangeDateChanging(value);
                    this.SendPropertyChanging();
                    this._LastChangeDate = value;
                    this.SendPropertyChanged("LastChangeDate");
                    this.OnLastChangeDateChanged();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter1", DbType = "Int")]
        public System.Nullable<int> Counter1
        {
            get
            {
                return this._Counter1;
            }
            set
            {
                if ((this._Counter1 != value))
                {
                    this.OnCounter1Changing(value);
                    this.SendPropertyChanging();
                    this._Counter1 = value;
                    this.SendPropertyChanged("Counter1");
                    this.OnCounter1Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter2", DbType = "Int")]
        public System.Nullable<int> Counter2
        {
            get
            {
                return this._Counter2;
            }
            set
            {
                if ((this._Counter2 != value))
                {
                    this.OnCounter2Changing(value);
                    this.SendPropertyChanging();
                    this._Counter2 = value;
                    this.SendPropertyChanged("Counter2");
                    this.OnCounter2Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter3", DbType = "Int")]
        public System.Nullable<int> Counter3
        {
            get
            {
                return this._Counter3;
            }
            set
            {
                if ((this._Counter3 != value))
                {
                    this.OnCounter3Changing(value);
                    this.SendPropertyChanging();
                    this._Counter3 = value;
                    this.SendPropertyChanged("Counter3");
                    this.OnCounter3Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter4", DbType = "Int")]
        public System.Nullable<int> Counter4
        {
            get
            {
                return this._Counter4;
            }
            set
            {
                if ((this._Counter4 != value))
                {
                    this.OnCounter4Changing(value);
                    this.SendPropertyChanging();
                    this._Counter4 = value;
                    this.SendPropertyChanged("Counter4");
                    this.OnCounter4Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter5", DbType = "Int")]
        public System.Nullable<int> Counter5
        {
            get
            {
                return this._Counter5;
            }
            set
            {
                if ((this._Counter5 != value))
                {
                    this.OnCounter5Changing(value);
                    this.SendPropertyChanging();
                    this._Counter5 = value;
                    this.SendPropertyChanged("Counter5");
                    this.OnCounter5Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter6", DbType = "Int")]
        public System.Nullable<int> Counter6
        {
            get
            {
                return this._Counter6;
            }
            set
            {
                if ((this._Counter6 != value))
                {
                    this.OnCounter6Changing(value);
                    this.SendPropertyChanging();
                    this._Counter6 = value;
                    this.SendPropertyChanged("Counter6");
                    this.OnCounter6Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter7", DbType = "Int")]
        public System.Nullable<int> Counter7
        {
            get
            {
                return this._Counter7;
            }
            set
            {
                if ((this._Counter7 != value))
                {
                    this.OnCounter7Changing(value);
                    this.SendPropertyChanging();
                    this._Counter7 = value;
                    this.SendPropertyChanged("Counter7");
                    this.OnCounter7Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter8", DbType = "Int")]
        public System.Nullable<int> Counter8
        {
            get
            {
                return this._Counter8;
            }
            set
            {
                if ((this._Counter8 != value))
                {
                    this.OnCounter8Changing(value);
                    this.SendPropertyChanging();
                    this._Counter8 = value;
                    this.SendPropertyChanged("Counter8");
                    this.OnCounter8Changed();
                }
            }
        }

        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_Counter9", DbType = "Int")]
        public System.Nullable<int> Counter9
        {
            get
            {
                return this._Counter9;
            }
            set
            {
                if ((this._Counter9 != value))
                {
                    this.OnCounter9Changing(value);
                    this.SendPropertyChanging();
                    this._Counter9 = value;
                    this.SendPropertyChanged("Counter9");
                    this.OnCounter9Changed();
                }
            }
        }

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void SendPropertyChanging()
        {
            if ((this.PropertyChanging != null))
            {
                this.PropertyChanging(this, emptyChangingEventArgs);
            }
        }

        protected virtual void SendPropertyChanged(String propertyName)
        {
            if ((this.PropertyChanged != null))
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
#pragma warning restore 1591