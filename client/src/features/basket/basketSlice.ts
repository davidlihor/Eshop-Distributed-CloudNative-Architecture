import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { Basket } from "../../app/models/basket";
import agent from '../../app/api/agent';

interface BasketState {
    basket: Basket | null
    loading: Boolean
}

const initialState: BasketState = {
    basket: null,
    loading: false
}

export const fetchBasketAsync = createAsyncThunk<Basket>('basket/fetchBasketAsync', async (_, thunkAPI) => {
    try {
        return await agent.Basket.get();
    } catch (error: any) {
        return thunkAPI.rejectWithValue({ error: error.data });
    }
})

export const deleteBasketItemAsync = createAsyncThunk<Basket, { productId: string }>
    ('basket/addBasketItemAsync', async ({ productId }, thunkAPI) => {
    try {
        return await agent.Basket.deleteItem(productId);
    } catch (error: any) {
        return thunkAPI.rejectWithValue({ error: error.data })
    }
})

export const updateBasketItemAsync = createAsyncThunk<Basket, { productId: string, quantity: number }>//@ts-ignore
    ('basket/updateBasketItemAsync', async ({ productId, quantity }, thunkAPI) => {
    try {
        const response = await agent.Basket.updateItem(productId, quantity);
        if(response.isSuccess == false) throw Error("Quantity Unavaible")
        return {
            isSuccess: response.isSuccess,
            quantity
        }
    } catch (error: any) {
        return thunkAPI.rejectWithValue({ error: error.data })
    }
})


export const basketSlice = createSlice({
    name: 'basket',
    initialState,
    reducers: {
        setBasket: (state, action) => {
            state.basket = action.payload
        },
        clearBasket: (state) => {
            state.basket = null;
        }
    },
    extraReducers: builder => {
        builder.addCase(fetchBasketAsync.fulfilled, (state, action) => {
            // @ts-ignore
            state.basket = action.payload.cart;
            state.loading = false;
        });
        builder.addCase(updateBasketItemAsync.pending, (state, action) => {
            const { productId, quantity } = action.meta.arg;
            state.loading = true;
            state.basket?.items.forEach(item => {
                if (item.productId === productId) {
                    item.oldQuantity = item.quantity; 
                    item.quantity = quantity;
                    item.isUpdating = true;
                }
            });
        });
        builder.addCase(updateBasketItemAsync.fulfilled, (state, action) => {
            const { productId } = action.meta.arg;
            state.basket?.items.forEach(item => {
                if (item.productId === productId) {
                    item.isUpdating = false;
                    delete (item as any).oldQuantity;
                }
            });
            state.loading = false;
        });
        builder.addCase(updateBasketItemAsync.rejected, (state, action) => {
            const { productId } = action.meta.arg;
            state.basket?.items.forEach(item => {
                if (item.productId === productId) {
                    if (item.oldQuantity !== undefined) {
                        item.quantity = item.oldQuantity;
                    }
                    delete (item as any).oldQuantity;
                    item.isUpdating = false;
                    item.updateError = true;
                }
            });
            state.loading = false;
        });
        builder.addCase(deleteBasketItemAsync.fulfilled, (state, action) => {
            const { productId } = action.meta.arg;
            if (state.basket) {
                state.basket.items = state.basket.items.filter(item => item.productId !== productId);
            }
        });
        builder.addCase(deleteBasketItemAsync.pending, (state, action) => {
            const { productId } = action.meta.arg;
            state.basket?.items.forEach(item => {
                if (item.productId === productId) {
                    item.isDeleteing = true
                }
            });
        });
        builder.addCase(deleteBasketItemAsync.rejected, (state, action) => {
            const { productId } = action.meta.arg;
            state.basket?.items.forEach(item => {
                if (item.productId === productId) {
                    item.isDeleteing = false
                }
            });
        });
    }
})

export const { setBasket, clearBasket } = basketSlice.actions;