import { Basket } from './basket';

export interface User {
    username: string;
    email: string;
    token: string;
    basket?: Basket;
    roles?: string[];
}