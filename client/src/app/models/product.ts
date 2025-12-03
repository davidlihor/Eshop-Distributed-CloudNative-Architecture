export interface Product {
    id: string
    title: string
    description: string
    pictureUrl: string
    price: number
    brand: string
    type?: string
    quantity: number
    category: string
}

export interface ProductParams {
    orderBy: string;
    searchTerm?: string;
    types: string[];
    brands: string[];
    pageNumber: number;
    pageSize: number;
}