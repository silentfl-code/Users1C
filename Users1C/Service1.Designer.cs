namespace Users1C
{
    partial class Service1
    {
        /// <summary> 
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
#if NEEDLOG
        public static System.Diagnostics.EventLog eventLog1 = new System.Diagnostics.EventLog();
#endif

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Обязательный метод для поддержки конструктора - не изменяйте 
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
#if NEEDLOG
            //this.eventLog1 = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(eventLog1)).BeginInit();
            // 
            // Service1
            // 
            this.ServiceName = "Users1C";
            ((System.ComponentModel.ISupportInitialize)(eventLog1)).EndInit();
#endif
        }
        #endregion

        //

    }
}
