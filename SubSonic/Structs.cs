


using System;
using SubSonic.Schema;
using System.Collections.Generic;
using SubSonic.DataProviders;
using System.Data;

namespace SubSonic {
	
        /// <summary>
        /// Table: Posts
        /// Primary Key: Id
        /// </summary>

        public class PostsTable: DatabaseTable {
            
            public PostsTable(IDataProvider provider):base("Posts",provider){
                ClassName = "Post";
                SchemaName = "dbo";
                

                Columns.Add(new DatabaseColumn("Id", this)
                {
	                IsPrimaryKey = true,
	                DataType = DbType.Int32,
	                IsNullable = false,
	                AutoIncrement = true,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Text", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.AnsiString,
	                IsNullable = false,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = -1
                });

                Columns.Add(new DatabaseColumn("CreationDate", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.DateTime,
	                IsNullable = false,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("LastChangeDate", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.DateTime,
	                IsNullable = false,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter1", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter2", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter3", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter4", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter5", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter6", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter7", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter8", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });

                Columns.Add(new DatabaseColumn("Counter9", this)
                {
	                IsPrimaryKey = false,
	                DataType = DbType.Int32,
	                IsNullable = true,
	                AutoIncrement = false,
	                IsForeignKey = false,
	                MaxLength = 0
                });
                    
                
                
            }

            public IColumn Id{
                get{
                    return this.GetColumn("Id");
                }
            }
				
   			public static string IdColumn{
			      get{
        			return "Id";
      			}
		    }
            
            public IColumn Text{
                get{
                    return this.GetColumn("Text");
                }
            }
				
   			public static string TextColumn{
			      get{
        			return "Text";
      			}
		    }
            
            public IColumn CreationDate{
                get{
                    return this.GetColumn("CreationDate");
                }
            }
				
   			public static string CreationDateColumn{
			      get{
        			return "CreationDate";
      			}
		    }
            
            public IColumn LastChangeDate{
                get{
                    return this.GetColumn("LastChangeDate");
                }
            }
				
   			public static string LastChangeDateColumn{
			      get{
        			return "LastChangeDate";
      			}
		    }
            
            public IColumn Counter1{
                get{
                    return this.GetColumn("Counter1");
                }
            }
				
   			public static string Counter1Column{
			      get{
        			return "Counter1";
      			}
		    }
            
            public IColumn Counter2{
                get{
                    return this.GetColumn("Counter2");
                }
            }
				
   			public static string Counter2Column{
			      get{
        			return "Counter2";
      			}
		    }
            
            public IColumn Counter3{
                get{
                    return this.GetColumn("Counter3");
                }
            }
				
   			public static string Counter3Column{
			      get{
        			return "Counter3";
      			}
		    }
            
            public IColumn Counter4{
                get{
                    return this.GetColumn("Counter4");
                }
            }
				
   			public static string Counter4Column{
			      get{
        			return "Counter4";
      			}
		    }
            
            public IColumn Counter5{
                get{
                    return this.GetColumn("Counter5");
                }
            }
				
   			public static string Counter5Column{
			      get{
        			return "Counter5";
      			}
		    }
            
            public IColumn Counter6{
                get{
                    return this.GetColumn("Counter6");
                }
            }
				
   			public static string Counter6Column{
			      get{
        			return "Counter6";
      			}
		    }
            
            public IColumn Counter7{
                get{
                    return this.GetColumn("Counter7");
                }
            }
				
   			public static string Counter7Column{
			      get{
        			return "Counter7";
      			}
		    }
            
            public IColumn Counter8{
                get{
                    return this.GetColumn("Counter8");
                }
            }
				
   			public static string Counter8Column{
			      get{
        			return "Counter8";
      			}
		    }
            
            public IColumn Counter9{
                get{
                    return this.GetColumn("Counter9");
                }
            }
				
   			public static string Counter9Column{
			      get{
        			return "Counter9";
      			}
		    }
            
                    
        }
        
}