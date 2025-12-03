export interface MetaData {
    pageIndex: number;
    pageSize: number;
    totalPages: number;
    totalCount: number;
}

export class PaginatedResponse<T> {
    items: T;
    metaData: MetaData;

    constructor(items: T, metaData: MetaData) {
        this.items = items;
        this.metaData = metaData;
    }
}