/*
Simple wrapper around UI Grid (http://ui-grid.info/docs)
We can add more properties as and when we need them
*/
module DataGrid {
	export class ColumnDef implements uiGrid.IColumnDef {
		field: string
		displayName: string
		enableColumnResizing = true
		constructor(fieldName: string, displayName?: string) {
			this.field = fieldName;
			this.displayName = displayName;
		}
	}
		
	export class GridOptions implements uiGrid.IGridOptions {
		columnDefs: any | Array<ColumnDef>
		data: Array<any> | string
		enableSorting = false
		showGridFooter = false
		enableGridMenu = true
		multiSelect = false      
      	enableSelectAll = false
		enableRowSelection = true
		enableRowHeaderSelection = false
		enableFullRowSelection = true
		enableHorizontalScrollbar = 0
		exporterMenuCsv = true
		exporterMenuPdf = false
		enableColumnMenus =false
		
		// Pagination related
		useExternalPagination = false
		paginationOptions = {
			"pageNumber" : 1,
			"pageSize" : 50,
			"sort" : "asc"
		}
	}
}