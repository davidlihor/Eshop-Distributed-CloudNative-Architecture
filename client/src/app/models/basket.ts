export interface BasketItem {
    productId: string;
    title: string;
    price: number;
    quantityAvaible: number;
    quantity: number;
    oldQuantity: number;
    pictureUrl: string;
    isUpdating?: boolean;
    isDeleteing?: boolean
    updateError?: boolean;
}

export interface Basket {
    userId: string;
    items: BasketItem[];
    totalPrice: string;
    couponCode: string;
}