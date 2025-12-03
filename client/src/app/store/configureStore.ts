import { TypedUseSelectorHook, useDispatch, useSelector } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { catalogSlice } from '../../features/catalog/catalogSlice';
import { basketSlice } from '../../features/basket/basketSlice';
import { counterSlice } from '../../features/contact/counterSlice';
import { accountSlice } from '../../features/account/accountSlice';

export const store = configureStore({
    reducer: {
        catalog: catalogSlice.reducer,
        basket: basketSlice.reducer,
        counter: counterSlice.reducer,
        account: accountSlice.reducer
    }
})

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;